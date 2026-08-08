using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.CinemaMode.Tests.Builders
{
    public class MovieBuilder
    {
        private Guid _id = Guid.NewGuid();
        private string _name = "Test Movie";
        private string _path = "/movies/test/test.mkv";
        private string[] _genres = Array.Empty<string>();
        private string[] _tags = Array.Empty<string>();
        private string[] _studios = Array.Empty<string>();
        private DateTime? _premiereDate;
        private int? _parentalRating;
        private int _localTrailerCount;

        public MovieBuilder WithId(Guid id) { _id = id; return this; }
        public MovieBuilder WithName(string name) { _name = name; return this; }
        public MovieBuilder WithPath(string path) { _path = path; return this; }
        public MovieBuilder WithGenres(params string[] genres) { _genres = genres; return this; }
        public MovieBuilder WithTags(params string[] tags) { _tags = tags; return this; }
        public MovieBuilder WithStudios(params string[] studios) { _studios = studios; return this; }
        public MovieBuilder WithPremiereDate(DateTime date) { _premiereDate = date; return this; }
        public MovieBuilder WithRating(int rating) { _parentalRating = rating; return this; }
        public MovieBuilder WithLocalTrailers(int count) { _localTrailerCount = count; return this; }

        public MovieBuilder WithYear(int year)
        {
            _premiereDate = new DateTime(year, 6, 15);
            return this;
        }

        public Movie Build()
        {
            var movie = new Movie
            {
                Id = _id,
                Name = _name,
                Path = _path,
                Genres = _genres,
                Tags = _tags,
                Studios = _studios,
                PremiereDate = _premiereDate,
                InheritedParentalRatingValue = _parentalRating,
            };

            if (_localTrailerCount > 0)
            {
                var extraIds = new List<Guid>();
                for (int i = 0; i < _localTrailerCount; i++)
                {
                    extraIds.Add(Guid.NewGuid());
                }
                movie.ExtraIds = extraIds.ToArray();
            }

            return movie;
        }

        /// <summary>
        /// Creates trailer BaseItem instances that correspond to a movie's ExtraIds.
        /// Call after Build(). Register these with the mock ILibraryManager via GetItemById.
        /// </summary>
        public static List<Video> CreateTrailerItems(Movie movie)
        {
            var trailers = new List<Video>();
            foreach (var extraId in movie.ExtraIds)
            {
                var trailer = new Video
                {
                    Id = extraId,
                    Path = $"/trailers/{extraId}.mkv",
                    ExtraType = ExtraType.Trailer,
                };
                trailers.Add(trailer);
            }
            return trailers;
        }
    }
}
