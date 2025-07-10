namespace Movies.Dejmenek.Exceptions;

public class ImageUploadException : Exception
{
    public ImageUploadException()
    {

    }

    public ImageUploadException(string message) : base(message)
    {

    }

    public ImageUploadException(string message, Exception? inner = null) : base(message, inner) { }
}
