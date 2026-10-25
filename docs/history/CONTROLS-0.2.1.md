> Historical document. Current instructions: [user guide](../../README.md), [active testing](../../TESTING-TODO.md).

# Controls: updated for 0.2.2

The old Ctrl+Alt shortcuts have been removed. They were not verified safe and did not work in the user's runtime. The user confirmed automatic rotation and seamless switching between grids in 0.2.

## Verified conflict evidence

Installed SE 1.210.014 source, under the game-code skill's Data/Decompiled:

- `Sandbox.Game/Sandbox/MySandboxGame.cs`: F8 = free spectator, F9 = static spectator, F10 = blueprints; Alt+F10 = admin menu, Shift+F10 = spawn menu. Arrow keys are rotation controls. PageUp/PageDown participate in block rotation bindings.
- `Sandbox.Game/Sandbox/Game/SessionComponents/MySessionComponentSpectatorTools.cs`: checks Ctrl held plus F8 held without excluding Alt. Thus Ctrl+Alt+F8 can enter a game handler too. Context/permissions still affect whether spectator functions act.
- The inspected `%APPDATA%/SpaceEngineers/SpaceEngineers.cfg` contains empty ControlsGeneral and ControlsButtons dictionaries. This establishes no saved overrides there, not proof of every in-memory binding or other plugin/OS shortcut.
- Online cross-check: https://spaceengineers.wiki.gg/wiki/Key_Bindings and https://spaceengineers.wiki.gg/wiki/Spectator_Mode.

The exact reason all original Marigold key presses were missed has not been reproduced. Modifier overlap, focus gating and input polling are separate concerns. We do not claim that every original combination necessarily triggered a default action. No replacement keyboard combination is claimed universally conflict-free.

## Replacement controls

While seated/controlling a ship, open chat using your normal chat key (Enter by default), type a command, and submit:

| Command | Action |
| --- | --- |
| `/mg` | Help |
| `/mg hide` / `/mg show` | Hide/show schematic |
| `/mg spin` | Toggle automatic rotation |
| `/mg reset` | Restore default cockpit-relative orientation, zoom and automatic spin |
| `/mg left` / `right` / `top` / `bottom` | Snap to the named cockpit-relative side; stop spin and preserve zoom |
| `/mg in` / `/mg out` | Zoom one step |
| `/mg zoom 1.5` | Set zoom, range 0.25–3 |

Use the full `/mg` prefix for each command. Responses appear locally. Commands work even when the schematic is hidden or you leave a cockpit; the model itself requires a controlled grid. Bindings do not alter the game's controls or consume movement/function keys. Commands are attached during the world session and detached on unload/disposal. If another plugin claims the same command prefix earlier, that could interfere; no exhaustive audit of third-party plugins was performed.

## API and validation

`IMyUtilities.MessageEntered` supplies a mutable sendToOthers flag. MyGuiScreenChat.OnInputFieldActivated calls MyAPIUtilities.EnterMessage before SendChatMessage and sends only when that flag stays true. Marigold suppresses its own prefix before parsing, including invalid commands; unrelated chat retains its incoming flag. IMyUtilities.ShowMessage adds a local HUD chat message. MyChatCommandSystem.CanHandle only claims registered commands, so unregistered slash commands reach this event in the base game.

The game/mod API and compiled assembly signatures are verified. Automated tests cover command state and send suppression. Runtime command entry, interaction with third-party chat handlers and display feedback still need in-game confirmation.

0.2.2: up/down remain aliases for top/bottom. The active controller Orientation.GetMatrix supplies grid-local right/up/backward axes. Cached points are expressed in that basis before projection; switching cockpits updates the basis without rebuilding geometry. Top and bottom views keep the cockpit nose toward the top of the display. Automated tests cover all 24 orthogonal cockpit orientations and repeated snap commands; actual in-game viewpoint selection still needs confirmation.


0.4.7: top/bottom presets swapped following user in-game feedback that they appeared inverted. Up/down follow top/bottom. Both views keep the nose at the top of the panel; left/right and reset remain unchanged.
