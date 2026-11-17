# SMInspectorPro Showcase

Open `Scenes/Showcase.unity` and select **Spawner** to see most attributes in one inspector: groups, conditional fields, buttons with parameters, lists with element labels, a table, a dictionary, a type picker and an inline editor.

`Data/Goblin.asset` and `Data/Orc.asset` are `EnemyProfile` assets with tabs, a preview, a dropdown, enum buttons, a progress bar and a range slider. The Orc is broken on purpose: it has no prefab and its starting health is above its max health. Open **Window > SMPro > Validator** and press Validate to find both.

Two example tool windows:

- **Window > SMPro > Showcase > Enemy Database**: an `SMMenuWindow` listing every `EnemyProfile`.
- **Window > SMPro > Showcase > Balance Tool**: an `SMEditorWindow` that scales the health of many profiles at once.
