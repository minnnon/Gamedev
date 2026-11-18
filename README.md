# Склеп трьох рун (Crypt3D)

Тривимірна фентезі-гра від першої/третьої особи, зроблена в Unity в межах курсу «C# Unity для написання ігор» (лабораторні 10–17).

Лицар прокидається в закинутому склепі. Щоб відчинити рунну браму й вибратися, треба знайти три руни, розв'язати кілька головоломок і пережити зустріч зі скелетами та Вартовим склепу.

## Керування

| Дія | Клавіша |
|---|---|
| Рух | W A S D |
| Біг | Shift |
| Стрибок | Пробіл |
| Атака мечем | Ліва кнопка миші |
| Блок щитом | Права кнопка миші (витрачає витривалість) |
| Взаємодія (двері, скрині, жаровня, руни) | E |
| Інвентар | Tab |
| Зілля лікування | Q |
| Кинути предмет / підняти | G |
| Вид від першої/третьої особи | V або C |
| Пауза, налаштування гучності | Esc |
| Лічильник FPS (налагодження) | F3 |
| Запис заміру у Console і CSV (налагодження) | F4 |

## Як грати

1. У головному меню обери «Нова гра» або «Продовжити» (кнопка активна, якщо є збереження).
2. Досліджуй склеп, збирай монети, зілля й руни, відкривай скрині.
3. Біля жаровні-чекпоінта натисни **E**: гра збереже прогрес, відновить здоров'я й запам'ятає місце відродження.
4. Вставити три руни в постаменти, відчинити рунну браму й дійти до виходу.
5. Після смерті можна почати з чекпоінта, завантажити збереження або вийти в меню.

## Запуск

**Готова збірка:** Build/Crypt3D.exe (Windows x64). Встановлення не потрібне.

**З вихідного проєкту:**

- Unity 6000.3.23f1, Universal Render Pipeline (URP);
- пакети: Input System, AI Navigation 2.0.14, ProBuilder 6.1.4;
- відкрити проєкт у Unity Hub, відкрити сцену `Assets/_Project/Scenes/MainMenu.unity` і натиснути Play.

Збереження лежить у `Application.persistentDataPath/crypt_save.json` (Windows: `%USERPROFILE%\AppData\LocalLow\<Company>\Crypt3D\`).

## Структура проєкту

```
Assets/
  _Project/
    Scenes/        MainMenu, Crypt
    Scripts/
      Core/        GameManager, SaveSystem, SaveData, ItemDatabase, Checkpoint, ExitZone, PerfOverlay
      Player/      PlayerController, PlayerBlock, PlayerRespawn
      CameraRig/   ThirdPersonCamera, FirstPersonViewModel
      Combat/      Health, PlayerCombat
      Enemies/     EnemyAI (скінченний автомат), EnemyCombat
      Interaction/ Interactor, Door, Chest, Lever, PressurePlate, SpikeTrap, Pushable, RuneSocket, RuneGate
      Inventory/   Inventory, Pickup
      Audio/       AudioManager, AudioHooks, EnemyAudio
      UI/          HUD, UIManager, MainMenu
    Resources/     Audio (звукові ефекти й музика), ItemDatabase
    Prefabs, Materials, Animations
  Editor/          AutoSave
  ThirdParty/      KayKit-пакети (див. нижче)
Docs/              TestPlan.md (тест-план, заміри, таблиця помилок)
```

## Архітектура коротко

- **Події замість зв'язності:** `Health.Changed/Died`, `PlayerCombat.Swung/Landed`, `PlayerBlock.BlockedHit`, `RuneGate.Opened`, `Inventory.Changed`. Звук і інтерфейс підписуються на події, а не опитують об'єкти.
- **GameManager:** одинак (`DontDestroyOnLoad`), стани Menu / Playing / Paused / GameOver / Win. Пауза — через `Time.timeScale = 0`.
- **Зберігання:** `JsonUtility` + `persistentDataPath`. Об'єкти ідентифікуються шляхом в ієрархії сцени.
- **Вороги:** NavMeshAgent і скінченний автомат (Patrol → Chase → Attack → Return), стан «заціпеніння» (stagger) із вікном стійкості, щоб не можна було заспамити ворога ударами.
- **Оптимізація:** static batching, occlusion culling, запечене непряме освітлення, стиснення текстур і звуку.

## Ресурси й ліцензії

| Ресурс | Автор | Ліцензія |
|---|---|---|
| KayKit — Dungeon Remastered | Kay Lousberg | CC0 |
| KayKit — Adventurers (лицар) | Kay Lousberg | CC0 |
| KayKit — Skeletons | Kay Lousberg | CC0 |
| KayKit — Character Animations | Kay Lousberg | CC0 |
| Звуки й музика | згенеровані процедурно Python-скриптами (numpy/scipy) для цього проєкту | власні |

Код гри написано для курсу; пакети Unity (Input System, AI Navigation, ProBuilder) використовуються за ліцензією Unity.

## Відомі обмеження

- Стан скелетів (здоров'я) та пасток/важелів не зберігається: після завантаження збереження вони в початковому стані.
- Звуки синтезовані, тому звучать простіше за студійні.
- Збірка перевірена лише на Windows x64.

## Автор

