namespace Grovetracks.Etl.Data;

public static class ComposableSubjects
{
    public static IReadOnlyList<string> All { get; } = new List<string>
    {
        // Animals
        "cat", "dog", "bird", "fish", "rabbit", "horse", "elephant", "lion",
        "butterfly", "snake", "turtle", "owl", "penguin", "dolphin", "whale",
        "bear", "fox", "deer", "frog", "octopus", "bee", "ladybug", "shark",
        "duck", "parrot", "crab", "spider", "snail", "bat", "flamingo",

        // Plants & Nature
        "tree", "flower", "cactus", "mushroom", "leaf", "palm tree", "sunflower",
        "rose", "pine tree", "bush", "acorn", "tulip", "daisy", "fern", "vine",
        "bamboo", "lotus", "seaweed", "clover", "cloud", "sun", "moon", "star",
        "mountain", "rainbow",

        // Food & Drink
        "apple", "banana", "pizza", "cake", "ice cream", "coffee cup", "cupcake",
        "donut", "hamburger", "hot dog", "cookie", "watermelon", "grapes",
        "pineapple", "cherry", "lollipop", "bread", "egg", "carrot", "broccoli",
        "sushi", "taco", "pretzel", "wine glass", "teapot",

        // Vehicles & Transport
        "car", "bicycle", "airplane", "boat", "train", "bus", "helicopter",
        "rocket", "skateboard", "motorcycle", "sailboat", "hot air balloon",
        "truck", "submarine", "canoe", "scooter", "tractor", "fire truck",
        "wagon", "sled",

        // Buildings & Places
        "house", "castle", "lighthouse", "church", "tent", "barn", "skyscraper",
        "windmill", "bridge", "igloo", "pyramid", "pagoda", "cabin", "tower",
        "gazebo",

        // Objects & Tools
        "key", "clock", "book", "umbrella", "chair", "lamp", "scissors",
        "hammer", "guitar", "piano", "drum", "camera", "telephone", "lightbulb",
        "pencil", "paintbrush", "ladder", "anchor", "compass", "crown",
        "trophy", "bell", "candle", "envelope", "globe", "magnifying glass",
        "telescope", "binoculars", "hourglass", "wheelbarrow",

        // Wearables & Accessories
        "hat", "glasses", "shoe", "boot", "glove", "ring", "necklace",
        "backpack", "watch", "scarf", "tie", "belt", "headphones", "purse",
        "helmet",

        // Sports & Activities
        "basketball", "soccer ball", "football", "baseball bat", "tennis racket",
        "surfboard", "kite", "fishing rod", "bowling pin", "volleyball",
        "dumbbell", "dart", "boomerang", "medal", "trophy cup",

        // Sea & Water
        "seashell", "starfish", "coral", "wave", "ship wheel", "buoy",
        "trident", "jellyfish", "seahorse", "pearl",

        // Fantasy & Fun
        "dragon", "unicorn", "ghost", "skull", "alien", "robot", "wizard hat",
        "magic wand", "treasure chest", "crystal ball", "sword", "shield",
        "potion bottle", "spaceship", "crown jewels"
    }.AsReadOnly();
}
