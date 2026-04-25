# Detialed Game Flow Design

## 1. Game Overview

**Genre:** 2D, Story-driven, Puzzle, Mild Horror  
**Age Rating:** 16+  
**Core Themes:** Life above all, Never give up, Stay true to yourself
**Duration:** 20 ~ 40 min

---

## 2. Character Setup and Profile

### Monster Faction
| Character | Description |
|-----------|-------------|
| **Eyeball Flower** | Plant, regular flower size, slender leaves, eyeball at center, flesh-like texture throughout. Plant with flower center shaped like an eyeball and flesh-like texture throughout. Transforms into a Giant Man-Eating Flower after absorbing energy stones |
| **Giant Man-Eating Flower** | 2-3 meters tall, closed bud-shaped flower head, contains hallucinogenic mucus. Can engulf prey in one bite—victims enter a hallucinatory state; those who lose their will to survive never wake up |

**Goal:** Occupy the base, capture the protagonist (to study humans in return)

### Protagonist Faction
| Character | Role |
|-----------|------|
| **Kian** | **Protagonist.**  An ordinary explorer—optimistic and steady, committed to his mission. After the incident, his sole focus is getting home |
| **Amelia** | **Kian's wife.** Stays by his side after the incident, waiting for him to regain consciousness |
| **Jeff** | **Kian's colleague.** Brought Eyeball Flowers back to the base for cultivation, mistakenly believing they were harmless |
| **Elena** | **Kian's colleague.** First to successfully wake up after the incident. Tries to call Kian, warning him not to trust anything in this world and never give up |

**Goal:** Open the portal and return home

---

## 3. Main Storyline

Protagonist Kian and his team are documenting their work on a new planet filled with unknown creatures. Preparing to return home, they plan to use three energy stones to open the portal. At the moment of activation, Jeff's bouquet of Eyeball Flowers (kept as a souvenir) devours one energy stone and transforms into multiple Giant Man-Eating Flowers, swallowing the entire team.

Kian enters a hallucinatory world from the flower's mucus. In this world, he finds himself alone in the base, full of confusion, with no memory of what happened during the portal activation. Deep down, he knows one thing: he must find his wife and the spare energy stones, then open the portal to return home.

To find the energy stones, Kian must gather clues and solve various puzzles. He receives both real and fake communications. He finds one energy stone by cracking a safe code in the bedroom. Two more remain—how will he obtain them?

### 4. Story Branches

**Path A: Open the Base Door**
- Monsters are waiting outside
- Instant death upon opening → **Ending 1: Death**

**Path B: Explore Further**
- Find a spare energy stone in the Control Room
- Continue exploring the Control Room's monitors and portal structure → Story continues

**Path C: Portal Exploration**  
If Kian continues exploring the portal and touches its structure, he discovers it feels like flesh, not metal → **Boss Battle ensues**

**Boss Battle Outcomes:**
- Kian is killed by the Boss → **Ending 1: Death**
- Kian kills the Boss without finding the special device. The portal opens without energy stones, but Kian walks through to find a mirror in darkness. Removing his helmet, he sees his head covered in tentacles → **Ending 2: Loop**

**Special Equipment Required for True Ending:**
Kian must find a special wearable display device. With this, he can see the Boss's heart movement during battle and predict attacks. After killing the Boss's heart and extracting an energy stone, Kian wakes up in the real world. His wife and colleagues are around him, having just pulled him from a withered flower. A final energy stone is revealed. Some colleagues didn't survive—their consciousness died. Kian's last energy stone provides enough power for the survivors to return home. → **Ending 3: Successfully Return Home**

---

## 5. Map Design

| Location | Connections/Notes |
|----------|-------------------|
| **Dormitory** | Leads to Dormitory Corridor |
| **Dormitory Corridor** | Left → Base Gate; Right → Elevator |
| **Base Gate** | Left side of Dormitory Corridor |
| **Elevator** | Right side of Dormitory Corridor; goes to B1 |
| **B1 Corridor** | Left → Archive Room; Right → Control Room |
| **Archive Room** | Left side of B1 Corridor |
| **Control Room** | Right side of B1 Corridor |
| **Portal Room** | Inside the Control Room |

---

## 6. Game Flow

### Opening Animation [1 min]
Introduces Kian's memory and current situation:

1. You and your colleagues work in a dangerous world filled with strange creatures—dangerous, desolate, home to unidentifiable alien life *(Image 1)*
2. After long exploration, you've completed your research. All data has been transmitted back to headquarters *(Image 2)*
3. Then you received the message: you can finally go home *(Image 3)*
4. You and your wife stand before the portal. Together, you press the activation button *(Image 4)*
5. ...That's the last thing you remember *(Black screen)*
6. You wake up alone. No colleagues. No wife. No response. Your journal lies beside you. The last line reads: "Finally, we can go home" *(Image 5)*
7. Despite all questions, only one thing matters—going home *(Image 6)*

---

### Phase 1: Understand the Situation in the Bedroom [6 min]

**Location:** Dormitory  
**Starting Items:** Journal, Pager  
**Collectible Items:** Jeff's ID Card (unlocks Archive Room), Jeff's Manuscripts (Eyeball Flower info)

**Observable Items:**
- Jeff's manuscripts (character/world building)
- Jeff's manuscript with his first day date = safe code
- Vase with Eyeball Flower (hints at lurking monster)
- Painting replacing window (oppression of base, longing for home)
- Jeff's safe → **First energy stone obtained**
- Pager: Can play wife's message (explains everyone went out, complicated situation, tells Kian to call if he wakes up). Outgoing calls don't connect

**Emotional Arc:** Confusion/Disorientation → Hope from wife's message → Curiosity/Unease

---

### Phase 2: Explore Map—Corridor, Gate, Elevator, Archive Room [10 min]

**Location:** Dormitory Corridor

**Destinations:**
- Two other dorm rooms (both empty)
- Left → Base Gate (Kian won't go out yet subconsciously)
- Right → Elevator → to B1
- B1 Corridor Center → Elevator
- B1 Corridor Left → Archive Room (unlock with Jeff's ID Card)
- B1 Corridor Right → Control Room (locked, needs higher clearance)

**Observable Items:**
- Employee rules beside Base Gate ("Base must never be left unattended")
- Vase with Eyeball Flower beside Base Gate
- Vase with Eyeball Flower beside Control Room entrance
- Eyeball Flower documents in Archive Room (time-estimation characteristics)

**Triggered Event:**
- First call from Elena (poor signal, asking what happened)

**Emotional Arc:** Confusion → Eerie sensation

---

### Phase 3: The Choice—Stay or Leave the Base

**Location:** Archive Room → Gate/Control Room

**Triggered Events:**
- Exiting Archive Room → Second message from wife (Jeff's betrayal)
- After 30 seconds → Third message from wife (urging Kian to go outside) + Second message from Elena (creating conflict)

**Decision Tree:**

| Choice | Result |
|--------|--------|
| Go to Base Gate | Hesitate at employee rules → **Decision: Go outside?** → **Ending 1: Death** [4 min] / Or figure out what happened first → Go to Control Room |
| Go to Control Room | Fourth message from wife (she urgently needs Kian's help) + Third message from Elena → **Critical choice:** Go outside? → **Ending 1: Death** [8 min] / Or check Control Room first → **Phase 4** |

**Emotional Arc:**
- After wife's third message: Suspicion → Contradiction → Anger → Calm
- After Elena's second message: Confusion → Tension → Worry
- After wife's fourth message: Unease → Hesitation
- After Elena's third message: Anxiety → Urgency

---

### Phase 4: The Truth in the Control Room

**Location:** Control Room entrance → Control Room → Portal Room entrance

**Observable Items:**

**Control Room Entrance:**
- Control Room door (no response to knocking)
- Card reader (Kian & Jeff's cards—insufficient clearance)
- Vase with Eyeball Flower → Break card reader cover → Circuit connection puzzle (easy, 1 min) → Enter Control Room

**Inside Control Room:**
- Cabinet → Spare energy stone → **Second energy stone obtained**
- Equipment wall (choices affect speed):
  - Knife (higher single damage to Boss) → **Leads to Ending 2: Loop**
  - Advanced armor (reduces damage per hit) → **Leads to Ending 2: Loop**
  - Anomaly scanner (reveals true weakness) → **May lead to Ending 3**
- Portal Room door (reinforced, needs higher clearance)
- Four computers (different colleagues):
  - Monitor control computer → Find correct date file → **Surveillance footage** (shows everyone at portal, Jeff's Eyeball Flower bouquet touching energy stone and transforming, people being swallowed)
  - Computer with card reader → Upgrade ID clearance

**Triggered Events:**
- After watching footage and choosing equipment: Fourth message from Elena → Upgrade ID card → Enter Portal Room → **Phase 5**

**Emotional Arc:**
- After watching footage: Realization → Horror
- After Elena's fourth message: Urgency → Forced calm → Courage → Longing for home

---

### Phase 5: Boss Battle

**Location:** Inside the Portal Room

---

## 7. The Message System (Real vs. Fake)

### Wife's Calls (Fake)
**Purpose:** Trick Kian into leaving the base—monsters waiting at the gate for instant kill

**Strategy:** Mislead Kian into believing Elena (who brought Eyeball Flowers to base) sabotaged the team to claim credit for herself, pushing Kian to leave the base.

*Elena is the specimen collection specialist skilled in biochemistry—normally bold, curious, eccentric. It wouldn't be impossible for her to knock everyone out at the moment of departure.*

Wife claims she and other colleagues went out searching for energy stones, leaving Kian in the base since he wouldn't wake up. Now that he's awake, he should join them.

| Message | Effect |
|---------|--------|
| "Kian, are you there? Can you hear me?" | Kian gains hope to continue exploring |
| "We woke up and Elena was gone. All three energy stones are missing. The portal shows signs of use... If we want to go home, we need to gather energy stones again." | Kian gets angry → Takes Elena's notes to find answers |
| "Kian, we're all outside looking for energy stones. It's daytime, very safe. If you hear this, come join us." | Kian wants to go out but remembers training rules → Hesitates → Goes to check Control Room |
| "Don't worry about the Control Room. Everything's normal. Just come over." | Kian is suspicious (no one answers the door) → Gathers clues → Successfully enters Control Room |
| "Kian, we're in danger! I lost my ID card. Open the door and help me!" | Kian receives colleague's call → Torn between two sides |

---

### Jeff's Calls (Real)
**Context:** Jeff was the first to escape the Man-Eating Flower. Some rescued colleagues are still in hallucinations; others have already lost their will to live. The flower that swallowed Kian is unusually large (it contained the energy stone).

Jeff can't pull Kian out of the flower, so he calls out continuously, trying to help Kian escape the hallucination.

| Message (with interference symbols) | Interpretation |
|-------------------------------------|----------------|
| "Kian, **can you** hear me? Don't **look at** those **things**! Wake **up**!" | Don't believe what you see—wake up! |
| "It **was Elena's Eyeball Flower**... everyone **got**...!" | Fragmentary warning |
| "We **got Elena out** of the **flower**... didn't expect she still **died**..." | They rescued Elena, but she still died (Kian may misinterpret as Elena betraying everyone) |
| "Your wife hasn't woken **up** yet. **Wake up**, she needs you!" | Urgent plea |
| "Wake up, Kian. Your wife will break down if you don't." | Shocking revelation |

---

### Documents (Real)

**Eyeball Flower Documents:**  
Eyeball closes during daytime, gradually "opens" as it gets dark, fully open at night—can estimate time.

**Elena's Notes:**  
Same-species creatures on this planet seem to share a neural system. Burning one Eyeball Flower triggers reactions from all nearby Eyeball Flowers → Explains how flowers inside base communicate with outside monsters.

**Elena's Notes (Discovery Log):**  
Records the day she discovered the Eyeball Flower. She wonders if Lip Flowers or Nose Flowers exist too—creatures on this planet seem assembled from familiar parts. She's excited to discover more.

**Kian's Training Manual:**  
The base must never be left unattended unless abandoned.

---

### Contradictions

| # | Contradiction |
|---|----------------|
| 1 | Elena isn't that type of person in Kian's memory |
| 2 | Training manual principle: Base must never be left unattended. Even if Elena escaped alone, the whole team wouldn't leave the base empty |
| 3 | Corridor Eyeball Flowers are fully open, but wife says it's daytime outside |
| 4 | Jeff says Elena died; Wife says Elena escaped alone |
| 5 | Jeff says wife hasn't woken up → Kian questions: then who has been talking to me? Or worries wife is in danger |
| 6 | If Elena wanted to betray everyone, why didn't she take the energy stone from the safe? |

---