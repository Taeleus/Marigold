# Blueprint formats

Marigold 0.14 reads XML ship blueprints named `bp.sbc`, including gzip-compressed XML, from local files, locally cached Workshop folders and the game's Cloud XML storage. The reader enforces size, XML depth and block-count limits, rejects external entities and never modifies the source.

## Not supported in this release

Protobuf-only `bp.sbcB5`, ZIP and Workshop `.bin` archives are not imported or included as searchable XML results. Supplying one as a path shows an explicit unsupported-format message. A file merely renamed to `.sbc` is not converted into XML.

Use the game's blueprint UI to save a local copy, then look for its XML `bp.sbc` under `%APPDATA%/SpaceEngineers/Blueprints/local`. Select that file in Marigold. If the game only produces a PB file, obtain/export the XML from the blueprint author; Marigold cannot convert it in this release. For an archive containing XML, extract it yourself to a separate folder and select the contained `bp.sbc`.

## Investigation

The locally inspected `Sandbox.Game/GUI_UPPER/MyBlueprintUtils.cs` has PB support through `MyObjectBuilderSerializerKeen.DeserializePB`. However, `LoadPrefab` can create/update a `B5` cache while loading XML or recovering a failed cache. `LoadWorkshopPrefab` consults subscribed Workshop state and may create cache directories/write PB data. Calling these convenience methods would violate Marigold's read-only source-file behavior.

Direct stream deserialization could be a future implementation, but the full object-builder path and archive handling are not integrated or validated here. We deliberately retain the existing bounded XML parser rather than claim PB compatibility. No source files, caches or world data were changed during this investigation.
