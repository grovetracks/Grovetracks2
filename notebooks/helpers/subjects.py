"""Composable subjects list — port of ComposableSubjects.cs."""

COMPOSABLE_SUBJECTS: list[str] = [
    # Animals
    "cat", "dog", "bird", "fish", "rabbit", "horse", "elephant", "lion",
    "butterfly", "snake", "turtle", "owl", "penguin", "dolphin", "whale",
    "bear", "fox", "deer", "frog", "octopus", "bee", "ladybug", "shark",
    "duck", "parrot", "crab", "spider", "snail", "bat", "flamingo",

    # Plants & Nature
    "tree", "flower", "cactus", "mushroom", "leaf", "palm tree", "sunflower",
    "rose", "pine tree", "bush", "acorn", "tulip", "daisy", "fern", "vine",
    "bamboo", "lotus", "seaweed", "clover", "cloud", "sun", "moon", "star",
    "mountain", "rainbow",

    # Food & Drink
    "apple", "banana", "pizza", "cake", "ice cream", "coffee cup", "cupcake",
    "donut", "hamburger", "hot dog", "cookie", "watermelon", "grapes",
    "pineapple", "cherry", "lollipop", "bread", "egg", "carrot", "broccoli",
    "sushi", "taco", "pretzel", "wine glass", "teapot",

    # Vehicles & Transport
    "car", "bicycle", "airplane", "boat", "train", "bus", "helicopter",
    "rocket", "skateboard", "motorcycle", "sailboat", "hot air balloon",
    "truck", "submarine", "canoe", "scooter", "tractor", "fire truck",
    "wagon", "sled",

    # Buildings & Places
    "house", "castle", "lighthouse", "church", "tent", "barn", "skyscraper",
    "windmill", "bridge", "igloo", "pyramid", "pagoda", "cabin", "tower",
    "gazebo",

    # Objects & Tools
    "key", "clock", "book", "umbrella", "chair", "lamp", "scissors",
    "hammer", "guitar", "piano", "drum", "camera", "telephone", "lightbulb",
    "pencil", "paintbrush", "ladder", "anchor", "compass", "crown",
    "trophy", "bell", "candle", "envelope", "globe", "magnifying glass",
    "telescope", "binoculars", "hourglass", "wheelbarrow",

    # Wearables & Accessories
    "hat", "glasses", "shoe", "boot", "glove", "ring", "necklace",
    "backpack", "watch", "scarf", "tie", "belt", "headphones", "purse",
    "helmet",

    # Sports & Activities
    "basketball", "soccer ball", "football", "baseball bat", "tennis racket",
    "surfboard", "kite", "fishing rod", "bowling pin", "volleyball",
    "dumbbell", "dart", "boomerang", "medal", "trophy cup",

    # Sea & Water
    "seashell", "starfish", "coral", "wave", "ship wheel", "buoy",
    "trident", "jellyfish", "seahorse", "pearl",

    # Fantasy & Fun
    "dragon", "unicorn", "ghost", "skull", "alien", "robot", "wizard hat",
    "magic wand", "treasure chest", "crystal ball", "sword", "shield",
    "potion bottle", "spaceship", "crown jewels",
]

SUBJECT_CATEGORIES: dict[str, list[str]] = {
    "Animals": COMPOSABLE_SUBJECTS[0:30],
    "Plants & Nature": COMPOSABLE_SUBJECTS[30:55],
    "Food & Drink": COMPOSABLE_SUBJECTS[55:80],
    "Vehicles & Transport": COMPOSABLE_SUBJECTS[80:100],
    "Buildings & Places": COMPOSABLE_SUBJECTS[100:115],
    "Objects & Tools": COMPOSABLE_SUBJECTS[115:145],
    "Wearables & Accessories": COMPOSABLE_SUBJECTS[145:160],
    "Sports & Activities": COMPOSABLE_SUBJECTS[160:175],
    "Sea & Water": COMPOSABLE_SUBJECTS[175:185],
    "Fantasy & Fun": COMPOSABLE_SUBJECTS[185:200],
}
