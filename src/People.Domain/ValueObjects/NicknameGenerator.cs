namespace People.Domain.ValueObjects;

public static class NicknameGenerator
{
    private static readonly string[] Adjectives =
    [
        "admiring",
        "adoring",
        "affectionate",
        "agitated",
        "amazing",
        "angry",
        "awesome",
        "backstabbing",
        "berserk",
        "big",
        "boring",
        "clever",
        "cocky",
        "compassionate",
        "condescending",
        "cranky",
        "desperate",
        "determined",
        "distracted",
        "dreamy",
        "drunk",
        "eager",
        "ecstatic",
        "elastic",
        "elated",
        "elegant",
        "evil",
        "fervent",
        "focused",
        "furious",
        "gigantic",
        "gloomy",
        "goofy",
        "grave",
        "happy",
        "high",
        "hopeful",
        "hungry",
        "infallible",
        "jolly",
        "jovial",
        "kickass",
        "lonely",
        "loving",
        "mad",
        "modest",
        "naughty",
        "nauseous",
        "nostalgic",
        "peaceful",
        "pedantic",
        "pensive",
        "prickly",
        "reverent",
        "romantic",
        "sad",
        "serene",
        "sharp",
        "sick",
        "silly",
        "sleepy",
        "small",
        "stoic",
        "stupefied",
        "suspicious",
        "tender",
        "thirsty",
        "tiny",
        "trusting",
        "zen"
    ];

    private static readonly string[] Animals =
    [
        "ant",
        "badger",
        "bear",
        "beaver",
        "bison",
        "cougar",
        "crane",
        "dolphin",
        "falcon",
        "fox",
        "gecko",
        "heron",
        "jaguar",
        "koala",
        "lemur",
        "lynx",
        "otter",
        "owl",
        "panda",
        "panther",
        "puffin",
        "raven",
        "tiger",
        "wolf"
    ];

    public static Nickname Create()
    {
        var adjective = Adjectives[Random.Shared.Next(Adjectives.Length)];
        var animal = Animals[Random.Shared.Next(Animals.Length)];

        var nickname = string.Create(
            adjective.Length + animal.Length + 1,
            (adjective, animal),
            static (buffer, state) =>
            {
                state.adjective.AsSpan().CopyTo(buffer);
                buffer[state.adjective.Length] = '_';
                state.animal.AsSpan().CopyTo(buffer[(state.adjective.Length + 1)..]);
            }
        );

        return Nickname.Parse(nickname);
    }
}
