using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Movies.Dejmenek.Data;
using Movies.Dejmenek.Enums;
using Movies.Dejmenek.Helpers;
using Movies.Dejmenek.Models;
using Movies.Dejmenek.Services;

namespace Movies.Dejmenek.Controllers
{
    [Authorize]
    public class MoviesController : Controller
    {
        private readonly MovieContext _context;
        private readonly IImageUploadService _imageUploadService;
        private readonly ILogger<MoviesController> _logger;
        private const int _pageSize = 4;

        public MoviesController(MovieContext context, ILogger<MoviesController> logger, IImageUploadService imageUploadService)
        {
            _context = context;
            _blobService = blobService;
            _logger = logger;
            _imageUploadService = imageUploadService;
        }

        // GET: Movies
        public async Task<IActionResult> Index(
            string movieGenre,
            string movieRating,
            int sortOption,
            string searchString,
            int? pageNumber
        )
        {
            if (_context.Movies == null) return Problem("Entity set 'MvcMovieContext.Movie' is null.");

            IQueryable<string> genreQuery = from m in _context.Movies
                                            orderby m.Genre
                                            select m.Genre;

            IQueryable<string> ratingsQuery = from m in _context.Movies
                                              orderby m.Rating
                                              select m.Rating;

            var movies = from m in _context.Movies
                         select m;

            if (!string.IsNullOrEmpty(searchString))
            {
                pageNumber = 1;
                movies = movies.Where(m => m.Title!.ToUpper().Contains(searchString.ToUpper()));
            }

            if (!string.IsNullOrEmpty(movieGenre))
            {
                movies = movies.Where(m => m.Genre == movieGenre);
            }

            if (!string.IsNullOrEmpty(movieRating))
            {
                movies = movies.Where(m => m.Rating == movieRating);
            }

            movies = (SortOptions)sortOption switch
            {
                SortOptions.TitleDesc => movies.OrderByDescending(m => m.Title),
                SortOptions.DateDesc => movies.OrderByDescending(m => m.ReleaseDate),
                SortOptions.DateAsc => movies.OrderBy(m => m.ReleaseDate),
                SortOptions.PriceDesc => movies.OrderByDescending(m => m.Price),
                SortOptions.PriceAsc => movies.OrderBy(m => m.Price),
                _ => movies.OrderBy(m => m.Title),
            };

            var movieGenreVM = new MovieGenreViewModel
            {
                MovieGenre = movieGenre,
                MovieRating = movieRating,
                SortOption = (SortOptions)sortOption,
                Genres = new SelectList(await genreQuery.Distinct().ToListAsync()),
                Ratings = new SelectList(await ratingsQuery.Distinct().ToListAsync()),
                Movies = await PaginatedList<Movie>.CreateAsync(movies.AsNoTracking(), pageNumber ?? 1, _pageSize)
            };

            return View(movieGenreVM);
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,ReleaseDate,Genre,Price,Rating,ImageFile")] CreateMovieViewModel createMovie)
        {
            if (!ModelState.IsValid)
            {
                return View(createMovie);
                }

            string? imageUri = null;

            try
            {
                imageUri = await _imageUploadService.UploadAsync(createMovie.ImageFile);

                var movie = new Movie
                {
                    Title = createMovie.Title,
                    ReleaseDate = createMovie.ReleaseDate,
                    Genre = createMovie.Genre,
                    Price = createMovie.Price,
                    Rating = createMovie.Rating,
                    ImageUri = imageUri
                };

                _context.Add(movie);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (ImageUploadException ex)
            {
                _logger.LogWarning(ex, "Image upload failed.");
                ModelState.AddModelError("", "There was a problem uploading the image. Please try again.");
                return View(createMovie);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "An error occurred while saving the movie.");

                if (imageUri != null)
                {
                    try
                    {
                        await _imageUploadService.DeleteAsync(imageUri);
                    }
                    catch (ImageDeleteException deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to delete uploaded image after failure.");
                    }
                }

                ModelState.AddModelError(string.Empty, "An error occurred while saving the movie.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");

                if (imageUri != null)
                {
                    try
                    {
                        await _imageUploadService.DeleteAsync(imageUri);
                    }
                    catch (ImageDeleteException deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to delete uploaded image after failure.");
                    }
                }

                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
            }

            return View(createMovie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }

            var editMovie = new EditMovieViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                ReleaseDate = movie.ReleaseDate,
                Genre = movie.Genre,
                Price = movie.Price,
                Rating = movie.Rating,
                ImageUri = movie.ImageUri
            };

            return View(editMovie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ReleaseDate,Genre,Price,Rating,ImageFile,ImageUri,RemoveImage")] EditMovieViewModel editMovie)
        {
            if (id != editMovie.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid) return View(editMovie);

            string? newImageUri = null;
            string? oldImageUri = editMovie.ImageUri;

            if (editMovie.ImageFile != null)
            {
                try
                {
                    newImageUri = await _imageUploadService.UploadAsync(editMovie.ImageFile);

                    if (!string.IsNullOrWhiteSpace(oldImageUri))
                        await _imageUploadService.DeleteAsync(oldImageUri);

                    editMovie.ImageUri = newImageUri;
                }
                catch (ImageUploadException ex)
                {
                    _logger.LogWarning(ex, "Image upload failed during Edit.");
                    ModelState.AddModelError("", "Failed to upload image. Please try again.");
                    return View(editMovie);
                }
            }
            else if (editMovie.RemoveImage && !string.IsNullOrWhiteSpace(oldImageUri))
            {
                try
                {
                    await _imageUploadService.DeleteAsync(oldImageUri);
                    editMovie.ImageUri = null;
                }
                catch (ImageDeleteException ex)
                    {
                    _logger.LogWarning(ex, "Image deletion failed during Edit.");
                    ModelState.AddModelError("", "Failed to remove image. Please try again.");
                    return View(editMovie);
                    }
            }

            try
            {
                    var movie = new Movie
                    {
                        Id = editMovie.Id,
                        Title = editMovie.Title,
                        ReleaseDate = editMovie.ReleaseDate,
                        Genre = editMovie.Genre,
                        Price = editMovie.Price,
                        Rating = editMovie.Rating,
                        ImageUri = editMovie.ImageUri
                    };

                    _context.Update(movie);
                    await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                if (!string.IsNullOrWhiteSpace(newImageUri))
                {
                    try
                    {
                        await _imageUploadService.DeleteAsync(newImageUri);
                        editMovie.ImageUri = oldImageUri;
                    }
                    catch (ImageDeleteException deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to delete uploaded image after failure.");
                    }
                }

                    if (!MovieExists(editMovie.Id))
                    {
                    _logger.LogError("Movie not found. Movie ID: {MovieId}", editMovie.Id);
                        return NotFound();
                    }
                    else
                    {
                    _logger.LogError("Concurrency error updating movie. Movie ID: {MovieId}", editMovie.Id);
                }

                ModelState.AddModelError(string.Empty, "An error occurred while saving the movie.");
                return View(editMovie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating movie. Movie ID: {MovieId}", editMovie.Id);

                if (!string.IsNullOrWhiteSpace(newImageUri))
                {
                    try
                    {
                        await _imageUploadService.DeleteAsync(newImageUri);
                        editMovie.ImageUri = oldImageUri;
                    }
                    catch (ImageDeleteException deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to delete uploaded image after failure.");
                }
            }

                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
            return View(editMovie);
        }
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                if (movie.ImageUri != null) await _blobService.DeleteAsync(movie.ImageUri);
                _context.Movies.Remove(movie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movies.Any(e => e.Id == id);
        }
    }
}
