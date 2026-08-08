using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.CinemaMode.Tests.Fixtures
{
    public class JellyfinFixture : IDisposable
    {
        public Mock<ILibraryManager> LibraryManager { get; }

        public JellyfinFixture()
        {
            LibraryManager = new Mock<ILibraryManager>();
            BaseItem.LibraryManager = LibraryManager.Object;
        }

        /// <summary>
        /// Registers trailer items so that Movie.LocalTrailers resolves them
        /// via BaseItem.LibraryManager.GetItemById().
        /// </summary>
        public void RegisterTrailerItems(params BaseItem[] items)
        {
            foreach (var item in items)
            {
                LibraryManager
                    .Setup(m => m.GetItemById(item.Id))
                    .Returns(item);
            }
        }

        public void Dispose()
        {
        }
    }

    [CollectionDefinition("Jellyfin")]
    public class JellyfinCollection : ICollectionFixture<JellyfinFixture>
    {
    }
}
