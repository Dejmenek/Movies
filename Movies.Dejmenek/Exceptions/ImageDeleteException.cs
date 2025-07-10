namespace Movies.Dejmenek.Exceptions;

public class ImageDeleteException : Exception
{
    public ImageDeleteException()
    {

    }

    public ImageDeleteException(string message) : base(message)
    {

    }

    public ImageDeleteException(string message, Exception? inner = null) : base(message, inner) { }
}
