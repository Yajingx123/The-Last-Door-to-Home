import matplotlib.pyplot as plt
import matplotlib.patches as patches
from pathlib import Path
import textwrap

plan = {
    "Week 1\nCore Framework": [
        ("Day 1", "Create Unity 2D project; set up Grid, Tilemap, folders"),
        ("Day 2", "Implement player movement, collision, camera follow"),
        ("Day 3", "Build interaction system: press E to inspect objects"),
        ("Day 4", "Add inventory items: ID card and energy stone"),
        ("Day 5", "Add scene transitions between rooms"),
        ("Day 6", "Add message trigger system with text popups"),
        ("Day 7", "Prototype ending: leaving base causes death"),
    ],
    "Week 2\nMain Flow": [
        ("Day 8", "Build Archive Room and ID-card access logic"),
        ("Day 9", "Add Control Room door puzzle"),
        ("Day 10", "Add second energy stone and scanner item"),
        ("Day 11", "Add monitor interaction and truth reveal"),
        ("Day 12", "Add ID permission upgrade system"),
        ("Day 13", "Add Teleport Room access logic"),
        ("Day 14", "Test full playable main route"),
    ],
    "Week 3\nBoss Mechanics": [
        ("Day 15", "Create Boss scene and spawn logic"),
        ("Day 16", "Implement Boss Phase 1 attack pattern"),
        ("Day 17", "Add player HP and damage system"),
        ("Day 18", "Implement Boss Phase 2 attacks"),
        ("Day 19", "Add weak-point visibility mechanic"),
        ("Day 20", "Add win and lose conditions"),
        ("Day 21", "Test and balance Boss fight"),
    ],
    "Week 4\nStory & UI": [
        ("Day 22", "Write opening story text"),
        ("Day 23", "Write Stage 1–3 message text"),
        ("Day 24", "Write Control Room reveal text"),
        ("Day 25", "Write ending text drafts"),
        ("Day 26", "Improve dialogue UI"),
        ("Day 27", "Add inventory UI"),
        ("Day 28", "Test full story flow"),
    ],
    "Week 5\nArt Replacement": [
        ("Day 29", "Draw floor and wall tiles"),
        ("Day 30", "Draw dorm furniture: bed and desk"),
        ("Day 31", "Draw corridor props"),
        ("Day 32", "Draw Control Room objects"),
        ("Day 33", "Draw Teleport Room objects"),
        ("Day 34", "Replace placeholder art"),
        ("Day 35", "Unify color palette"),
    ],
    "Week 6\nAudio & Polish": [
        ("Day 36", "Add walking sound"),
        ("Day 37", "Add door and interaction sound"),
        ("Day 38", "Add background music"),
        ("Day 39", "Fix gameplay bugs"),
        ("Day 40", "Improve UI feedback"),
        ("Day 41", "Playtest full game"),
        ("Day 42", "Prepare final build"),
    ],
}

week_goals = {
    "Week 1": "Goal: Setup engine and core movement.",
    "Week 2": "Goal: Complete environment logic.",
    "Week 3": "Goal: Implement Boss mechanics.",
    "Week 4": "Goal: Finalize narrative & UI.",
    "Week 5": "Goal: Complete Art replacement.",
    "Week 6": "Goal: Final Polish and Release."
}

TITLE = "The Last Door to Home"
SUBTITLE = "Six-Week Development Plan"
OUTPUT_PATH = Path("docs/images/six_week_plan.png")

BG_COLOR = "#F6F4EF"
CARD_COLOR = "#FFFFFF"
DAY_CARD_COLOR = "#F9F7F2"
GOAL_COLOR = "#F0EEE7"

TEXT_COLOR = "#263238"
SUBTEXT_COLOR = "#5F6F73"

WEEK_COLORS = [
    "#5B7C99",
    "#6B8F71",
    "#A67C52",
    "#8A6F9E",
    "#B47B77",
    "#6D8A96",
]


def wrap_text(text, width=31):
    return "\n".join(textwrap.wrap(text, width))


def get_week_key(title):
    return title.split("\n")[0]


def draw_week_card(ax, x, y, w, h, title, tasks, color):
    shadow = patches.FancyBboxPatch(
        (x + 0.04, y - 0.04),
        w,
        h,
        boxstyle="round,pad=0.02,rounding_size=0.08",
        linewidth=0,
        facecolor="#000000",
        alpha=0.10,
    )
    ax.add_patch(shadow)

    card = patches.FancyBboxPatch(
        (x, y),
        w,
        h,
        boxstyle="round,pad=0.02,rounding_size=0.08",
        linewidth=1.3,
        edgecolor="#DDD6CC",
        facecolor=CARD_COLOR,
    )
    ax.add_patch(card)

    header_h = 0.72
    header = patches.FancyBboxPatch(
        (x, y + h - header_h),
        w,
        header_h,
        boxstyle="round,pad=0.02,rounding_size=0.08",
        linewidth=0,
        facecolor=color,
    )
    ax.add_patch(header)

    ax.text(
        x + w / 2,
        y + h - header_h / 2,
        title,
        ha="center",
        va="center",
        fontsize=14,
        color="white",
        fontweight="bold",
        linespacing=1.05,
    )

    # Goal box
    week_key = get_week_key(title)
    goal_text = week_goals.get(week_key, "")

    goal_h = 0.38
    goal_x = x + 0.20
    goal_w = w - 0.40
    goal_y = y + h - header_h - 0.52

    goal_box = patches.FancyBboxPatch(
        (goal_x, goal_y),
        goal_w,
        goal_h,
        boxstyle="round,pad=0.012,rounding_size=0.035",
        linewidth=0.8,
        edgecolor="#E1D8CB",
        facecolor=GOAL_COLOR,
    )
    ax.add_patch(goal_box)

    ax.text(
        goal_x + 0.14,
        goal_y + goal_h / 2,
        goal_text,
        ha="left",
        va="center",
        fontsize=8.7,
        fontweight="bold",
        color=color,
    )

    # Day cards
    item_h = 0.405
    gap = 0.055

    item_x = x + 0.20
    item_w = w - 0.40

    start_y = goal_y - 0.16

    for i, (day, task) in enumerate(tasks):
        item_y = start_y - (i + 1) * item_h - i * gap

        day_box = patches.FancyBboxPatch(
            (item_x, item_y),
            item_w,
            item_h,
            boxstyle="round,pad=0.01,rounding_size=0.028",
            linewidth=0.75,
            edgecolor="#E1D8CB",
            facecolor=DAY_CARD_COLOR,
        )
        ax.add_patch(day_box)

        ax.text(
            item_x + 0.13,
            item_y + item_h / 2,
            day,
            ha="left",
            va="center",
            fontsize=8.8,
            fontweight="bold",
            color=color,
        )

        ax.text(
            item_x + 1.02,
            item_y + item_h / 2,
            wrap_text(task, width=34),
            ha="left",
            va="center",
            fontsize=7.85,
            color=TEXT_COLOR,
            linespacing=0.95,
        )


def generate_plan():
    fig, ax = plt.subplots(figsize=(18, 12.6), facecolor=BG_COLOR)
    ax.set_xlim(0, 18)
    ax.set_ylim(0, 12.6)
    ax.axis("off")

    ax.text(
        9,
        12.05,
        TITLE,
        ha="center",
        va="center",
        fontsize=30,
        fontweight="bold",
        color=TEXT_COLOR,
    )

    ax.text(
        9,
        11.65,
        SUBTITLE,
        ha="center",
        va="center",
        fontsize=16,
        color=SUBTEXT_COLOR,
    )

    card_w = 5.3
    card_h = 4.85

    x_positions = [0.7, 6.35, 12.0]
    y_positions = [6.25, 0.95]

    items = list(plan.items())

    for idx, ((title, tasks), color) in enumerate(zip(items, WEEK_COLORS)):
        row = idx // 3
        col = idx % 3

        draw_week_card(
            ax=ax,
            x=x_positions[col],
            y=y_positions[row],
            w=card_w,
            h=card_h,
            title=title,
            tasks=tasks,
            color=color,
        )

    ax.text(
        9,
        0.35,
        "Goal: Build a playable vertical slice first, then polish art, story, and audio.",
        ha="center",
        va="center",
        fontsize=11.5,
        color=SUBTEXT_COLOR,
    )

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    plt.savefig(OUTPUT_PATH, dpi=300, bbox_inches="tight")
    print("Image generated:", OUTPUT_PATH)


if __name__ == "__main__":
    generate_plan()