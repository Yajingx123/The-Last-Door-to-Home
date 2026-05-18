import matplotlib.pyplot as plt
import matplotlib.patches as patches
from matplotlib.font_manager import FontProperties
from pathlib import Path

plan = {
    "Week 1\nEnding 1 Slice": [
        ("Day 1", "Build all non-Boss scenes and connect room transitions"),
        ("Day 2", "Complete DormRoom gameplay: ID card, notes, safe, energy stone"),
        ("Day 3", "Complete ArchiveRoom gameplay and ID-card access logic"),
        ("Day 4", "Implement message trigger flow and gate decision logic"),
        ("Day 5", "Implement Ending 1 and test the full route"),
        ("Day 6", "Replace key placeholder tiles with basic styled environment art"),
    ],
    "Week 2\nFull Main Route": [
        ("Day 1", "Implement Control Room entrance puzzle"),
        ("Day 2", "Add second energy stone and Control Room core interactions"),
        ("Day 3", "Implement monitor system and truth reveal sequence"),
        ("Day 4", "Implement ID permission upgrade and Teleport Room access"),
        ("Day 5", "Create simple Boss prototype with weak-point logic"),
        ("Day 6", "Test full flow from opening to Teleport Room"),
    ],
    "Week 3\nBoss & Polish": [
        ("Day 1", "Improve Boss attack patterns and basic pacing"),
        ("Day 2", "Improve Scanner interaction and weak-point visibility"),
        ("Day 3", "Implement Ending 2 and Ending 3 conditions"),
        ("Day 4", "Redraw key assets for Control Room, Archive Room, and Teleport Room"),
        ("Day 5", "Improve dialogue UI, message readability, and ending UI"),
        ("Day 6", "Adjust story rhythm, trigger order, and player guidance"),
    ],
    "Week 4\nTesting & Atmosphere": [
        ("Day 1", "Conduct external playtest and record confusion points"),
        ("Day 2", "Fix critical progression, trigger, and scene transition bugs"),
        ("Day 3", "Add core sound effects for doors, elevator, pickup, and messages"),
        ("Day 4", "Add ambient sound, background music, and atmosphere effects"),
        ("Day 5", "Update README, devlog, credits, and GitHub project board"),
        ("Day 6", "Final build test, cleanup, and submission preparation"),
    ],
}

week_goals = {
    "Week 1": "Goal: Complete the first playable Ending 1 route.",
    "Week 2": "Goal: Make the full main route playable.",
    "Week 3": "Goal: Improve Boss, art, UI, and narrative flow.",
    "Week 4": "Goal: Test, fix, polish, and prepare submission.",
}

TITLE = "The Last Door to Home"
SUBTITLE = "Four-Week Development Plan"
OUTPUT_PATH = Path("docs/images/four_week_plan.png")

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
]


def wrap_text_to_data_width(ax, text, max_data_width, fontsize, fontweight="normal"):
    renderer = ax.figure.canvas.get_renderer()
    font_props = FontProperties(size=fontsize, weight=fontweight)

    x0 = ax.transData.transform((0, 0))[0]
    x1 = ax.transData.transform((max_data_width, 0))[0]
    max_px_width = abs(x1 - x0)

    words = text.split()
    if not words:
        return ""

    lines = []
    current = words[0]

    for word in words[1:]:
        trial = f"{current} {word}"
        trial_width = renderer.get_text_width_height_descent(
            trial, font_props, ismath=False
        )[0]
        if trial_width <= max_px_width:
            current = trial
        else:
            lines.append(current)
            current = word

    lines.append(current)
    return "\n".join(lines)


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

    item_x = x + 0.20
    item_w = w - 0.40

    top_gap = 0.16
    bottom_gap = 0.14
    gap = 0.06
    task_count = len(tasks)
    available_h = (goal_y - top_gap) - (y + bottom_gap)
    item_h = (available_h - (task_count - 1) * gap) / task_count
    start_y = goal_y - top_gap

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

        task_x = item_x + 1.02
        right_padding = 0.12
        task_max_w = (item_x + item_w) - right_padding - task_x

        ax.text(
            task_x,
            item_y + item_h / 2,
            wrap_text_to_data_width(
                ax=ax,
                text=task,
                max_data_width=task_max_w,
                fontsize=7.9,
            ),
            ha="left",
            va="center",
            fontsize=7.9,
            color=TEXT_COLOR,
            linespacing=0.95,
        )


def generate_plan():
    fig, ax = plt.subplots(figsize=(15, 11.2), facecolor=BG_COLOR)
    ax.set_xlim(0, 15)
    ax.set_ylim(0, 11.2)
    ax.axis("off")
    fig.canvas.draw()

    ax.text(
        7.5,
        10.65,
        TITLE,
        ha="center",
        va="center",
        fontsize=28,
        fontweight="bold",
        color=TEXT_COLOR,
    )

    ax.text(
        7.5,
        10.25,
        SUBTITLE,
        ha="center",
        va="center",
        fontsize=15,
        color=SUBTEXT_COLOR,
    )

    card_w = 6.4
    card_h = 4.35

    x_positions = [0.7, 7.9]
    y_positions = [5.5, 0.75]

    items = list(plan.items())

    for idx, ((title, tasks), color) in enumerate(zip(items, WEEK_COLORS)):
        row = idx // 2
        col = idx % 2

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
        7.5,
        0.28,
        "Goal: Build the full playable route early, then improve Boss, art, testing, audio, and final polish.",
        ha="center",
        va="center",
        fontsize=10.8,
        color=SUBTEXT_COLOR,
    )

    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    plt.savefig(OUTPUT_PATH, dpi=300, bbox_inches="tight")
    print("Image generated:", OUTPUT_PATH)


if __name__ == "__main__":
    generate_plan()
