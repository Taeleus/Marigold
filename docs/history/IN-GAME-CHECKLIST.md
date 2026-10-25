> Historical document. Current instructions: [user guide](../../README.md), [active testing](../../TESTING-TODO.md).

# Milestone 1 in-game acceptance

All items below are pending. Record game version, Pulsar version, runtime, result and relevant log excerpt when testing.

- [ ] Legacy loads the package and reports `[Marigold] Ship observer initialized (0.1.0)` without exceptions.
- [ ] Main menu and loading screens show no ship notification.
- [ ] Enter cockpit and flight seat: correct name and main-grid slim-block count.
- [ ] Exit to character: notification disappears promptly.
- [ ] Switch between two ships and between controllers on one ship: no stale name or duplicate notification.
- [ ] Rename the grid, add/remove armor and add/remove a multi-cell block: displayed count/name refresh correctly. Count multi-cell blocks once.
- [ ] Remote-control another grid, then release remote control: correct transitions.
- [ ] Connector docking does not add the docked ship's blocks. A mechanically connected subgrid is intentionally excluded in this milestone.
- [ ] Grid closure/destruction/merge/split: no exceptions or lingering notification. Confirm the displayed grid follows the active controller.
- [ ] Pause, hide HUD, open terminal and return: no disruptive input capture; document notification visibility behavior.
- [ ] Leave world and load another without restarting: clean removal and recovery.
- [ ] Multiplayer cockpit/remote tests: same behavior using available client state; no server component.
- [ ] Large-grid smoke test: no count-related full-grid scans or noticeable frame-time regression.
- [ ] Disable plugin/restart: no Marigold HUD. Check game log for observer or cleanup errors.

Do not claim this milestone is complete until these tests pass. Renderer feasibility is a separate milestone.
