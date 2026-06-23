const state = {
  mood: 0,
  anger: 0,
  tool: "hand",
  ended: false,
};

const reactions = {
  head: {
    hand: { mood: 2, anger: 0, text: "Head: the cat likes being touched here." },
    brush: { mood: 10, anger: 0, text: "Head: the cat enjoys being brushed here." },
  },
  back: {
    hand: { mood: 1, anger: 0, text: "Back: the cat relaxes a little." },
    brush: { mood: 10, anger: 0, text: "Back: smooth brushing builds trust." },
  },
  belly: {
    hand: { mood: 0, anger: 2, text: "Belly and leg: this is risky territory." },
    brush: { mood: 10, anger: 4, text: "Belly and leg: grooming helps, but the cat is uncomfortable." },
  },
  tail: {
    hand: { mood: 0, anger: 2, text: "Tail: the cat does not like that touch." },
    brush: { mood: 10, anger: 4, text: "Tail: brushing here makes the cat tense." },
  },
};

const catSprites = {
  normal: "../Assets/sprites/orange%20cat.png",
  slightHappy: "../Assets/sprites/orange%20cat%20slightly%20satisfy.png",
  happy: "../Assets/sprites/orange%20cat%20satisfy.png",
  slightAngry: "../Assets/sprites/orange%20cat%20slightly%20angry.png",
  angry: "../Assets/sprites/orange%20cat%20angry.png",
  hiss: "../Assets/sprites/orange%20cat%20haqi.png",
  sleep: "../Assets/sprites/orange%20cat%20sleep.png",
};

const moodFill = document.querySelector("#moodFill");
const angerFill = document.querySelector("#angerFill");
const moodValue = document.querySelector("#moodValue");
const angerValue = document.querySelector("#angerValue");
const reactionText = document.querySelector("#reactionText");
const demoCat = document.querySelector("#demoCat");
const resetButton = document.querySelector("#resetDemo");
const toolButtons = document.querySelectorAll(".tool");
const hotspots = document.querySelectorAll(".hotspot");

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function currentSprite() {
  if (state.anger >= 7 || state.mood >= 100) return catSprites.hiss;
  if (state.mood >= 70 && state.anger <= 3) return catSprites.sleep;
  if (state.anger >= 5) return catSprites.angry;
  if (state.anger >= 4) return catSprites.slightAngry;
  if (state.mood >= 60) return catSprites.happy;
  if (state.mood >= 38) return catSprites.slightHappy;
  return catSprites.normal;
}

function render() {
  moodFill.style.width = `${state.mood}%`;
  angerFill.style.width = `${(state.anger / 7) * 100}%`;
  moodValue.textContent = state.mood;
  angerValue.textContent = state.anger;
  demoCat.src = currentSprite();
  toolButtons.forEach((button) => {
    button.classList.toggle("active", button.dataset.tool === state.tool);
  });
}

function touchPart(part) {
  if (state.ended) return;

  const reaction = reactions[part][state.tool];
  state.mood = clamp(state.mood + reaction.mood, 0, 100);
  state.anger = clamp(state.anger + reaction.anger, 0, 7);

  if (state.anger >= 7) {
    state.ended = true;
    reactionText.textContent = "The cat hisses. Session failed: boundaries were pushed too far.";
  } else if (state.mood >= 70 && state.anger <= 3) {
    state.ended = true;
    reactionText.textContent = "The cat falls asleep. Session success: careful service paid off.";
  } else if (state.mood >= 100) {
    state.ended = true;
    reactionText.textContent = "The cat has had enough attention. Session failed: too much of a good thing.";
  } else {
    reactionText.textContent = reaction.text;
  }

  render();
}

toolButtons.forEach((button) => {
  button.addEventListener("click", () => {
    state.tool = button.dataset.tool;
    reactionText.textContent =
      state.tool === "hand"
        ? "Hand mode: observe the cat's habits."
        : "Brush mode: groom the places the cat seems to like.";
    render();
  });
});

hotspots.forEach((button) => {
  button.addEventListener("click", () => touchPart(button.dataset.part));
});

resetButton.addEventListener("click", () => {
  state.mood = 0;
  state.anger = 0;
  state.tool = "hand";
  state.ended = false;
  reactionText.textContent = "Use the hand to learn what this cat likes.";
  render();
});

render();
