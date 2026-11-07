# Refactor Application UI to Wrexercise Brand Identity

## Objective

Refactor the existing application to adopt the new **Wrexercise** visual identity.

Wrexercise is a social fitness challenge application where friends track their physical activities, compete toward weekly goals, exchange challenges, earn rewards and motivate each other.

The application should feel like:

**An urban streetwear brand meets an indie arcade game meets a friendly sports club.**

This is primarily a visual design and UX presentation refactor. Preserve existing functionality, business rules, data contracts, application architecture and integrations unless a change is strictly necessary to implement the new visual identity.

Do not rebuild the application from scratch.

Do not introduce unnecessary dependencies or architectural complexity.

---

# 1. Brand identity

## Brand personality

The visual identity must communicate five characteristics:

- **Urban:** Contemporary streetwear culture, graphic stickers, sports-court markings and expressive typography.
- **Friendly:** Approachable, social, welcoming and inclusive.
- **Competitive:** Friendly rivalry, playful challenges and a little trash talk.
- **Playful:** Arcade-inspired details, expressive illustrations and satisfying feedback.
- **Energetic:** Movement, progress, activity and achievement.

The application must never feel hostile, intimidating or overly aggressive.

Avoid conventional fitness-app aesthetics such as generic green dashboards, bodybuilding imagery, supplement branding and sterile corporate UI.

## Brand positioning

Wrexercise is not simply a workout tracker.

It is a place where friends:
- Track their physical progress.
- Challenge each other.
- Compete toward weekly goals.
- Celebrate achievements.
- Maintain consistency together.

The social experience and friendly competition are central to the product.

---

# 2. Design system

Implement a centralized design system using the application's existing styling architecture.

Do not scatter hardcoded colors, typography settings, radii or spacing values throughout components.

Create reusable design tokens and components.

## Color palette

Use the following palette as the source of truth.

| Token | Color | Hex | Purpose |
|---|---|---|---|
| Asphalt | Dark charcoal | #222633 | Primary dark surfaces |
| Chalk | Warm off-white | #F5F1E8 | Primary light surfaces |
| Coral | Warm orange-red | #FF684A | Primary brand accent and actions |
| Electric Violet | Purple | #7957E8 | Game elements and secondary accents |
| Signal Yellow | Yellow | #FFD34E | Achievements, rewards and highlights |
| Lime | Soft green | #A8D978 | Success and progress |
| Neutral Grey | Muted grey | #A9B0C2 | Secondary text and disabled states |

### Color usage

- Coral is the primary action color.
- Asphalt is the primary dark background.
- Chalk is the primary light background.
- Violet identifies game-related elements and interactions.
- Yellow highlights achievements and rewards.
- Lime communicates progress and success.

Do not use every accent color simultaneously in every component.

Use color intentionally to establish hierarchy.

### Light and dark themes

Support both light and dark appearances if the existing application already supports them.

If only one theme exists, implement the new identity in that theme without unnecessarily introducing a theme-management subsystem.

Light theme:
- Chalk backgrounds.
- Asphalt text.
- Coral primary actions.
- Subtle neutral surfaces.

Dark theme:
- Asphalt backgrounds.
- Chalk text.
- Slightly lighter charcoal surfaces.
- Coral and violet accents.

Maintain sufficient contrast and accessibility.

---

# 3. Typography

Use a two-family typography system.

### Display typography

Preferred: **Anton**

Alternative: **Barlow Condensed**

Use display typography for:
- Page titles.
- Large progress numbers.
- Challenge titles.
- Achievement names.
- Weekly rankings.
- Promotional elements.

Display typography should feel bold, condensed and expressive.

### Interface typography

Preferred: **Plus Jakarta Sans**

Use it for:
- Body text.
- Form fields.
- Buttons.
- Navigation.
- Labels.
- Supporting information.
- Notifications.

Avoid using display typography for long paragraphs or dense data.

Typography should have a clear hierarchy, with generous readability and consistent sizing.

Use the existing font-loading mechanism. Avoid introducing remote font dependencies at runtime if fonts can be bundled locally.

---

# 4. Visual language

Establish a recognizable visual language throughout the application.

## Graphic motifs

Use these elements selectively:

- Sticker-inspired badges.
- Thick outlines.
- Angular geometric shapes.
- Comic-book impact bursts.
- Halftone textures.
- Motion lines.
- Dashed sports-court markings.
- Numbered score labels.
- Small pixel-art details.
- Slightly irregular graphic shapes.

The overall interface must remain clean and usable.

Do not turn every card into a sticker or every screen into a game interface.

The graphic elements should reinforce important information, not compete with it.

## Illustration style

Use modern, expressive pixel art for:
- User avatars.
- Game environments.
- Achievements.
- Decorations.
- Empty states.
- Special events.

Keep ordinary interface elements modern and clean.

Use pixel art as a distinctive brand feature, not as a requirement for every icon or control.

Preserve existing avatar assets and game illustrations wherever possible.

Do not generate or introduce unrelated decorative assets without a clear purpose.

---

# 5. Component styling

Audit existing reusable UI components and refactor them to follow the new design system.

Prioritize:

### Buttons

Primary:
- Coral background.
- Dark or high-contrast text.
- Confident typography.
- Rounded but not excessively pill-shaped.
- Clear pressed and disabled states.

Secondary:
- Neutral or transparent backgrounds.
- Strong outline or subtle surface distinction.

Destructive:
- Use a semantic error color rather than repurposing coral automatically.

### Cards

- Use clean surfaces and subtle borders.
- Moderate corner radii.
- Clear information hierarchy.
- Avoid excessive shadows.
- Use accent colors for important information rather than entire card backgrounds.

### Badges and labels

Create consistent styles for:
- Achievement badges.
- Activity status.
- Challenge status.
- Rankings.
- Rewards.
- Weekly missions.

Badges should feel like small sports-club or arcade labels.

### Progress indicators

- Use circular progress indicators where appropriate.
- Use strong, readable numeric values.
- Coral and lime should communicate progress.
- Celebrate completion with a subtle visual effect.

### Navigation

- Maintain the existing navigation structure.
- Improve visual consistency.
- Use clear active-state indicators.
- Do not introduce new navigation destinations without a functional reason.

### Forms

- Maintain existing form behavior.
- Improve input hierarchy, spacing, labels and focus states.
- Ensure validation messages are easy to understand.

### Dialogs and notifications

- Make them visually consistent with the brand.
- Use expressive titles where appropriate.
- Preserve clear action labels.
- Avoid excessive animation or distracting effects.

---

# 6. Product-specific visual identity

The following areas should receive particular attention.

## Main application home

The home screen should immediately communicate:

1. The user's current weekly progress.
2. Their group's activity.
3. Pending actions or activities awaiting verification.
4. Relevant challenges and missions.
5. A clear action to register a new activity.

Prioritize the information users need to act on.

Do not sacrifice information density or usability for decorative elements.

## Jogo mode

This is the most expressive part of the Wrexercise experience.

The plaza scene should feel like a social gathering in an urban sports environment.

Maintain:
- Circular member arrangement.
- Pixel-art avatars.
- Avatar status animations.
- Achievement decorations.
- Weekly progress.
- Energy balance.
- Card hand.
- Pending activity review.
- Weekly missions.
- Rival of the week.

Use the new brand palette for UI elements around the game scene.

Preserve the existing game mechanics and card behavior.

The scene itself should feel like an urban playground rather than a fantasy RPG environment.

## Challenge cards

Cards are a defining product mechanic and should receive special visual attention.

Give each card:
- A recognizable visual identity.
- A clear title.
- A concise description.
- An appropriate category.
- A consistent visual hierarchy.

Use the Wrexercise graphic language:
- Bold titles.
- Strong borders.
- Graphic accents.
- Small pixel-art or illustrative symbols.

Differentiate Provocação and Resposta cards without making their appearance inconsistent.

Do not introduce new cards or alter existing card effects.

Do not reintroduce discarded defense cards.

## Achievements and rewards

Achievements should feel rewarding and collectible.

Use:
- Strong visual symbols.
- Signal Yellow for special achievements.
- Lime for progress and completion.
- Pixel-art illustrations where appropriate.
- Subtle celebration animations.

Avoid generic trophy dashboards.

---

# 7. Brand voice and microcopy

Refactor visible interface copy where appropriate to reflect the Wrexercise personality.

The voice should be:
- Casual.
- Confident.
- Playful.
- Encouraging.
- Slightly cheeky.
- Never condescending.

Examples:

| Context | Suggested copy |
|---|---|
| Challenge received | Someone's picking a fight. |
| Challenge accepted | Bring it! |
| Goal completed | You crushed it! |
| Achievement unlocked | Flex unlocked! |
| Weekly challenge | New week. New excuses. |
| Activity registered | Nice. Logged! |
| Rival defeated | That's gotta hurt. |
| Empty challenge list | Nobody's brave enough yet. |
| Activity reminder | Still time to get moving. |

Important:
- These are examples, not mandatory replacements.
- Preserve Portuguese as the application's primary language if that is the existing localization.
- Adapt expressions naturally to pt-BR instead of performing literal translations.
- Avoid excessive slang.
- Do not make users feel guilty for inactivity.
- Keep critical actions, errors and confirmations unambiguous.

Avoid changing copy that is part of business rules, external contracts or legally important disclosures.

---

# 8. Motion and interaction

Introduce subtle motion where it improves feedback.

Examples:
- Button press feedback.
- Small progress animations.
- Achievement unlock animations.
- Card interaction transitions.
- Subtle celebration effects.
- Smooth navigation transitions.

Animation guidelines:
- Keep transitions short and purposeful.
- Respect reduced-motion preferences.
- Avoid continuous decorative animation.
- Avoid unnecessary CPU or battery consumption.
- Preserve existing game animation behavior.
- Ensure interactions remain responsive.

Do not introduce a large animation framework solely for cosmetic effects.

---

# 9. Application icon and logo

Prepare the application to use the Wrexercise identity.

The logo direction should explore:
- A stylized flexed arm.
- A fist bump.
- A challenge card.
- A geometric W symbol that subtly resembles opposing flexed arms.

Avoid:
- Dumbbells.
- Sneakers.
- Generic gym equipment.
- Bodybuilding imagery.
- Aggressive combat imagery.

The logo must work at small sizes.

Use a temporary placeholder only if final logo assets are not available.

Do not invent a final logo without producing a corresponding asset.

Ensure the application name and metadata are updated consistently where appropriate, but do not rename repositories, namespaces, database entities or package identifiers unless explicitly requested.

---

# 10. Technical constraints

This is a visual refactor, not a backend rewrite.

Preserve:

- Existing architecture.
- Existing application boundaries.
- Business logic.
- Database schema.
- API contracts.
- Authentication and authorization.
- Existing navigation and routes.
- Existing validation rules.
- Existing integrations.
- Existing gamification rules.
- Existing fitness calculations.

Technical requirements:

- Reuse existing UI components wherever practical.
- Centralize design tokens.
- Avoid unnecessary dependencies.
- Avoid duplicating styling logic.
- Keep components maintainable.
- Preserve Native AOT compatibility if applicable.
- Preserve existing performance characteristics.
- Ensure layouts remain responsive.
- Support accessible contrast, focus states and touch targets.

Do not modify business behavior merely to make a screen look better.

---

# 11. Execution strategy

Work incrementally.

## Phase 1 — Audit

Inspect the existing repository and identify:

- UI framework and styling architecture.
- Existing design tokens.
- Existing reusable components.
- Current navigation structure.
- Main application screens.
- Jogo mode screens.
- Challenge and achievement components.
- Existing visual assets.
- Current localization strategy.

Create a concise audit before implementation.

## Phase 2 — Design system

Implement:
- Color tokens.
- Typography.
- Spacing.
- Radii.
- Borders.
- Shadows.
- Component states.
- Shared visual primitives.

Ensure the design system is reusable.

## Phase 3 — Core screens

Refactor the highest-visibility screens first:

1. Main application home.
2. Jogo home.
3. Group and member views.
4. Challenge cards and interactions.
5. Activity registration.
6. Progress and ranking.
7. Achievements.
8. Settings and secondary screens.

Preserve existing functionality throughout.

## Phase 4 — Polish

Review:
- Responsive layouts.
- Empty states.
- Loading states.
- Error states.
- Notifications.
- Accessibility.
- Animation.
- Light/dark appearance, where applicable.

Ensure all screens feel like the same product.

## Phase 5 — Validation

Run the existing test suite and relevant build commands.

Verify:
- No existing flows were broken.
- No business rules were modified.
- No API contracts changed.
- No navigation regressions.
- No missing assets.
- No AOT or compilation regressions.
- No obvious accessibility regressions.

Provide a summary of the changes.

---

# 12. Definition of done

The refactor is complete when:

- The application has a consistent Wrexercise visual identity.
- The color palette and typography are centralized.
- Core screens follow the new visual language.
- Jogo mode feels like an urban social arcade experience.
- Challenge cards are visually distinctive.
- Achievements feel rewarding.
- The interface remains clean and usable.
- Existing functionality and business rules remain intact.
- The application builds and existing tests pass.

## Final design principle

**Wrexercise should feel like your friends started a fitness club, turned it into a game, and gave everyone permission to talk a little trash.**

Make the application feel alive, social and fun — without compromising the usability of a real fitness application.