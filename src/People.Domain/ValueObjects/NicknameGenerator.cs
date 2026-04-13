namespace People.Domain.ValueObjects;

public static class NicknameGenerator
{
    private static readonly string[] Adjectives =
    [
        "admiring",
        "adoring",
        "affectionate",
        "amazing",
        "awesome",
        "bold",
        "bright",
        "calm",
        "clever",
        "compassionate",
        "daring",
        "determined",
        "dreamy",
        "eager",
        "ecstatic",
        "elastic",
        "elated",
        "elegant",
        "epic",
        "fearless",
        "fervent",
        "focused",
        "gentle",
        "gigantic",
        "golden",
        "graceful",
        "happy",
        "heroic",
        "hopeful",
        "infallible",
        "jolly",
        "jovial",
        "keen",
        "lively",
        "loving",
        "luminous",
        "mighty",
        "modest",
        "nimble",
        "noble",
        "nostalgic",
        "peaceful",
        "pedantic",
        "pensive",
        "radiant",
        "resilient",
        "reverent",
        "romantic",
        "serene",
        "sharp",
        "silly",
        "sleepy",
        "spirited",
        "stellar",
        "stoic",
        "swift",
        "tender",
        "tiny",
        "trusting",
        "valiant",
        "vibrant",
        "vivid",
        "witty",
        "zen"
    ];

    private static readonly string[] Nouns =
    [
        "ant",
        "aurora",
        "badger",
        "bear",
        "beaver",
        "bison",
        "cedar",
        "cheetah",
        "cobra",
        "comet",
        "condor",
        "cougar",
        "crane",
        "cypress",
        "dolphin",
        "dragon",
        "ember",
        "falcon",
        "fox",
        "frost",
        "gazelle",
        "gecko",
        "griffin",
        "hawk",
        "heron",
        "horizon",
        "jaguar",
        "koala",
        "kraken",
        "lemur",
        "lotus",
        "lynx",
        "mantis",
        "mustang",
        "nebula",
        "orchid",
        "osprey",
        "otter",
        "owl",
        "panda",
        "panther",
        "pegasus",
        "phoenix",
        "puffin",
        "quasar",
        "raven",
        "sparrow",
        "sphinx",
        "tiger",
        "viper",
        "wolf"
    ];

    public static Nickname Create()
    {
        var adjective = Adjectives[Random.Shared.Next(Adjectives.Length)];
        var noun = Nouns[Random.Shared.Next(Nouns.Length)];

        var nickname = string.Create(
            adjective.Length + noun.Length + 1,
            (adjective, noun),
            static (buffer, state) =>
            {
                state.adjective.AsSpan().CopyTo(buffer);
                buffer[state.adjective.Length] = '_';
                state.noun.AsSpan().CopyTo(buffer[(state.adjective.Length + 1)..]);
            }
        );

        return Nickname.Parse(nickname);
    }
}
