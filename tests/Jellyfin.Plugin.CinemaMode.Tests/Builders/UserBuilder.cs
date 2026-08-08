using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Plugin.CinemaMode.Tests.Builders
{
    public class UserBuilder
    {
        private string _username = "TestUser";
        private string _authProvider = "Jellyfin.Server.Implementations.Users.DefaultAuthenticationProvider";
        private string _passwordResetProvider = "Jellyfin.Server.Implementations.Users.DefaultPasswordResetProvider";

        public UserBuilder WithUsername(string username) { _username = username; return this; }

        public User Build()
        {
            return new User(_username, _authProvider, _passwordResetProvider);
        }
    }
}
