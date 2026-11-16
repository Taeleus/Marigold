# Blueprint formats

Marigold reads Space Engineers XML ship blueprints in plain or gzip-compressed `.sbc` files. **Baselines → Import blueprint → Find blueprint** searches any combination of Local, Workshop, and Cloud by part of the blueprint name. The browser uses these sources:

- **Local** searches `Blueprints/local` under the active game data folder, including a custom `-appdata` location. It includes nested folders and skips directory links.
- **Workshop** searches `bp.sbc` files already in the current game's Steam Workshop cache. It does not download Workshop items.
- **Cloud** lists `bp.sbc` files from the game's cloud service and reads the chosen file when you select it.

You can also enter a full `.sbc` path directly on the import page. Load the file, prepare the grid, and select the matching cockpit before choosing **Preview**. **Use baseline** saves the reference after confirmation. Marigold imports one selected grid. It resolves installed block definitions and lists missing definitions under **Omitted blocks**. Your ship, blueprint files, and Workshop cache are left as they were.

The parser accepts one ship blueprint per file, with up to 64 grids and 100,000 blocks across them. File input is limited to 32 MiB. XML is limited to 64 Mi characters and 64 levels of nesting, with DTDs and external entities disabled.

## Unsupported formats

For a protobuf-only `bp.sbcB5` or a Workshop `.bin` archive, try saving a local copy through the game and then look for `Blueprints/local/<blueprint>/bp.sbc` under the active game data folder. If the local copy contains only PB data, ask the author for the XML. ZIP and `.bin` archive files cannot be imported directly. For an archive that contains XML, extract it to a separate folder and select the included `bp.sbc`.

Compound block records, supergrids, and old blueprint records that store orientation only as a quaternion are outside the current import support. Saving an older blueprint in the current game can update its orientation format.

## Format investigation

The inspected Space Engineers 1.210.014 `MyBlueprintUtils.LoadPrefab` can create or update a `B5` cache while reading XML or recovering from a failed cache. `LoadWorkshopPrefab` can create cache directories and write PB data. Marigold uses its own bounded XML parser to read blueprints without these side effects. The investigation located `MyObjectBuilderSerializerKeen.DeserializePB`, but did not validate a separate read-only PB import path.
