# Changelog

## [1.0.0] - 2026-11-18

First release.

### Inspector
- Fallback editor for MonoBehaviour and ScriptableObject that draws types using SMInspectorPro attributes and leaves every other type to Unity's default inspector. `SMInspectorEditor` can be derived from by custom editors.
- Layout: `BoxGroup`, `FoldoutGroup`, `TabGroup`, `HorizontalGroup`, `VerticalGroup` nesting by path, and `PropertyOrder`.
- Decorators: `Title`, `InfoBox` (with `visibleIf`), `LabelText`, `HideLabel`, `Suffix`, `Indent`.
- Conditions: `ShowIf`, `HideIf`, `EnableIf`, `DisableIf` by member name and values, `ReadOnly`, `ShowInPlayMode`, `HideInPlayMode`. Groups hide when all their fields are hidden.
- Members: `Button` with parameters, undo and multi-object support; `ShowInInspector`; `OnValueChanged`.
- Values: `ValueDropdown`, `EnumToggleButtons`, `ProgressBar`, `MinMaxSlider`, `PreviewField`, `FilePath`, `FolderPath`.
- Collections: attributes inside list elements, `ListSettings`, `TableList`, and `SerializableDictionary<TKey, TValue>` drawn as a key and value table.
- References: `TypePicker` for `[SerializeReference]` fields and `InlineEditor`.

### Validation
- `Required`, `ValidateInput`, `MinValue`, `MaxValue`, `AssetsOnly`, `SceneObjectsOnly`, `ChildrenOnly` and `ISelfValidator`, shown under each field. Hidden fields are skipped.
- `SMValidator` and Window ▸ SMPro ▸ Validator for scenes, build scenes, prefabs and ScriptableObjects. Also reports attribute misuse, missing scripts and lost `[SerializeReference]` classes.
- Optional check before builds and a command-line entry for CI.

### Tools
- `SMEditorWindow` and `SMMenuWindow` with `MenuTree`.
- Project Settings ▸ SMPro ▸ Inspector.
- Public handler API: `FieldHandler<T>`, `DecoratorHandler<T>`, `StateHandler<T>`, `ValidatorHandler<T>`, `GroupHandler<T>`.

### Samples
- Showcase.
