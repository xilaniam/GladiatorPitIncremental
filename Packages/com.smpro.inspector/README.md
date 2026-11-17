# SMInspectorPro

Attribute-driven inspectors for Unity 6.

Full documentation, with measured performance: `Documentation~/index.html` (open it in a browser).

Add attributes to your fields and methods to get grouped layouts, conditional fields, buttons, validation, richer value pickers, tables and tool windows, without writing editor code. Classes that use no SMInspectorPro attribute keep Unity's default inspector, unchanged. Everything runs on UI Toolkit and Unity's own serialization, so removing the package never loses data.

## Install

1. Window ▸ Package Manager ▸ **+** ▸ **Add package from git URL** and paste:

   ```
   https://github.com/xilaniam/GladiatorPitIncremental.git?path=/Packages/com.smpro.inspector#inspector-v1.0.0
   ```

2. Optional: Package Manager ▸ SMInspectorPro ▸ Samples ▸ **Showcase** ▸ Import. Open `Scenes/Showcase.unity` and select **Spawner**.

## Quick start

```csharp
using SMPro.Inspector;

public class Spawner : MonoBehaviour
{
    [BoxGroup("Wave")] public bool useCurve;
    [BoxGroup("Wave"), ShowIf(nameof(useCurve))] public AnimationCurve countCurve;
    [BoxGroup("Wave"), HideIf(nameof(useCurve)), MinValue(1)] public int count = 5;

    [Required, AssetsOnly] public GameObject prefab;
    [ValueDropdown(nameof(SpawnPoints))] public string spawnPoint;

    [Button(ButtonSize.Large)] void SpawnNow(int extra = 0) { }
    IEnumerable<string> SpawnPoints => new[] { "North", "South" };
}
```

To keep your own custom editor and still use the attributes, derive it from `SMInspectorEditor`.

## Attributes

| Kind | Attributes |
|---|---|
| Layout | `BoxGroup`, `FoldoutGroup`, `TabGroup`, `HorizontalGroup`, `VerticalGroup`, `PropertyOrder` |
| Decorators | `Title`, `InfoBox`, `LabelText`, `HideLabel`, `Suffix`, `Indent` |
| Conditions | `ShowIf`, `HideIf`, `EnableIf`, `DisableIf`, `ReadOnly`, `ShowInPlayMode`, `HideInPlayMode` |
| Members | `Button`, `ShowInInspector`, `OnValueChanged` |
| Validation | `Required`, `ValidateInput`, `MinValue`, `MaxValue`, `AssetsOnly`, `SceneObjectsOnly`, `ChildrenOnly`, plus `ISelfValidator` |
| Values | `ValueDropdown`, `EnumToggleButtons`, `ProgressBar`, `MinMaxSlider`, `PreviewField`, `FilePath`, `FolderPath` |
| Collections | `ListSettings`, `TableList`, plus `SerializableDictionary<TKey, TValue>` |
| References | `TypePicker` for `[SerializeReference]`, `InlineEditor` |

Member names are best passed with `nameof`. A name can be negated (`"!flag"`) or nested (`"settings.enabled"`). It is looked up on the field's own class first, then on the classes that contain it. Conditions can compare values: `[ShowIf(nameof(mode), Mode.A, Mode.B)]`.

Attributes work inside nested serializable classes, list elements and `[SerializeReference]` objects when their class uses SMInspectorPro attributes. An attribute used on the wrong field type falls back to Unity's field and shows a red message.

## Validation

Messages show under their field and update after each change; `MinValue` and `MaxValue` clamp in the inspector. Fields hidden by a condition are not validated.

- **Window ▸ SMPro ▸ Validator** scans open scenes, build scenes, prefabs and ScriptableObjects. It reports rule failures, attribute misuse, missing scripts and lost `[SerializeReference]` classes.
- `SMValidator.Validate(object)` and `SMValidator.ValidateProject(options)` return the same results from code.
- `-executeMethod SMPro.Inspector.EditorTools.SMValidator.RunFromCommandLine` exits with code 1 on errors, for CI.
- **Validate before build** in the settings runs the scan before every build.

## Tool windows

- `SMEditorWindow`: derive from it and add fields and `[Button]` methods. The window draws itself with every attribute.
- `SMMenuWindow`: override `BuildMenu(MenuTree tree)` and add items by path, or `tree.AddAllAssetsOf<T>("Folder")`. You get a searchable tree with an inspector for the selected item.

## Settings

Project Settings ▸ SMPro ▸ Inspector has these options:

- turn the fallback inspector off;
- namespaces to skip;
- the poll interval for conditions that read properties or methods;
- the build check.

## Extending

```csharp
// Runtime assembly
public sealed class EvenAttribute : InspectorAttribute { }

// Editor assembly
[AttributeHandler(typeof(EvenAttribute))]
public sealed class EvenHandler : ValidatorHandler<EvenAttribute>
{
    public override void Validate(ValidationReport report)
    {
        if (Property.SerializedProperty.intValue % 2 != 0) report.Error($"{DisplayName} must be even");
    }
}
```

The handler base classes are `FieldHandler<T>`, `DecoratorHandler<T>`, `StateHandler<T>`, `ValidatorHandler<T>` and `GroupHandler<T>`. A handler in your project for a built-in attribute replaces the built-in one.

## Notes

- Unity allows one fallback editor per type. If another attribute package with its own fallback editor is installed, only one wins; turn ours off in the settings if needed.
- Buttons and `ShowInInspector` members come after the fields of their class unless `PropertyOrder` moves them.
- `SMPro.Inspector.FilePath` has the same name as `UnityEditor.FilePath`; editor code importing both namespaces must write one in full.
- Non-serialized values are read-only in 1.0.
