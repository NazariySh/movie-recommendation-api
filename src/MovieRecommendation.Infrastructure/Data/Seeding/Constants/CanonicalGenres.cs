namespace MovieRecommendation.Infrastructure.Data.Seeding.Constants;

public static class CanonicalGenres
{
    public static readonly IReadOnlyList<CanonicalGenre> All =
    [
        new("action", "Action", "Бойовик"),
        new("adventure", "Adventure", "Пригоди"),
        new("animation", "Animation", "Анімація"),
        new("children", "Children", "Дитячий"),
        new("comedy", "Comedy", "Комедія"),
        new("crime", "Crime", "Кримінал"),
        new("documentary", "Documentary", "Документальний"),
        new("drama", "Drama", "Драма"),
        new("family", "Family", "Сімейний"),
        new("fantasy", "Fantasy", "Фентезі"),
        new("film-noir", "Film-Noir", "Фільм-нуар"),
        new("history", "History", "Історичний"),
        new("horror", "Horror", "Жахи"),
        new("imax", "IMAX", "IMAX"),
        new("music", "Music", "Музичний"),
        new("musical", "Musical", "Мюзикл"),
        new("mystery", "Mystery", "Містика"),
        new("romance", "Romance", "Романтика"),
        new("science-fiction", "Science Fiction", "Наукова фантастика"),
        new("sci-fi", "Sci-Fi", "Наукова фантастика"),
        new("tv-movie", "TV Movie", "Телефільм"),
        new("thriller", "Thriller", "Трилер"),
        new("war", "War", "Воєнний"),
        new("western", "Western", "Вестерн"),
    ];
}
