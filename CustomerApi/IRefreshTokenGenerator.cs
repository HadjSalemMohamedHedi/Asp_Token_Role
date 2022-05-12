namespace CustomerApi
{
    public interface IRefreshTokenGenerator
    {
        string GenerateToken(string username);

    }
}
