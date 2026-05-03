# ClassicMode 维护概览（2026-05-02）

## 1. 项目结构速览
- `ClassicMode/`：Mod C# 源码与构建脚本。
- `ClassicMode/Patches/`：Harmony 补丁（UI、池切换、百科、行为修复）。
- `ClassicMode/Relics/`：自定义遗物实现（`ClassicRelic` 子类）。
- `ClassicMode/Pools/`：经典卡池/遗物池与混合池定义。
- `ClassicMode/assets/ClassicMode/localization/*/*.json`：**运行时实际使用**的本地化产物。
- `ClassicMode/prepare_assets.py`：资源与本地化生成入口（构建时会重写产物）。
- `desktop-1.0/`：STS1 解包资源（本地化和贴图来源）。
- `proj/`：STS2 源码（用于对照系统行为和池实现）。

## 2. 构建与部署链路（关键）
- 统一入口：`./classicmode-local-build.ps1`
- 过程：`dotnet build` -> `prepare_assets.py` -> Godot reimport -> 打包 pck -> 部署到游戏 `mods/ClassicModeMod`。
- 结论：
  - 不要直接手改 `assets/ClassicMode/localization/*.json`，下次构建会被覆盖。
  - 需要长期生效的本地化/资源映射，应该改 `prepare_assets.py`。

## 3. 遗物模式（当前）
- `AddClassicRelics`（添加经典遗物）：在 STS2 现有池上追加 STS1 非重复遗物。
- `OnlyClassicRelics`（仅经典遗物）：普通/罕见/稀有/商店池切到经典池。
- UI：选人页与自定义模式有两个互斥选项（“添加经典遗物”/“仅经典遗物”）。

## 4. 近期高风险点与已知细节
- Starter 升级遗物（如 `RingOfTheSerpent`、`FrozenCore`）若不在任何 pool 中，会在 hover/检视时触发 `RelicModel.Pool` 异常（日志关键字：`Sequence contains no matching element`）。
- 百科“锁定/未解锁”显示依赖 `UnlockState.Relics`，必要时需在聚合补丁里显式并入特殊遗物。
- `TouchOfOrobas.GetUpgradedStarterRelic` 已有仅经典模式覆盖补丁：
  - `RingOfTheSnake -> RingOfTheSerpent`
  - `CrackedCore -> FrozenCore`
- 图标与本地化命名不一致时需在 `prepare_assets.py` 做 alias（例如 `serpent_ring.png -> ringOfTheSerpent.png`）。

## 5. 建议后续维护顺序
1. 先确认“是否在 pool 中可追踪/可掉落”。
2. 再确认“本地化 key 与 SmartFormat 占位符是否匹配”。
3. 最后看“百科与检视（UnlockState + HoverTip）”。

---

## 6. STS1 Boss 遗物在 STS2 的状态与池分布
数据源：`sts1export/relics.md`（Boss 28 个） + `proj/MegaCrit.Sts2.Core.Models.Relics/*` + `proj/.../RelicPools/*` + 本 Mod 源码。

### 6.1 汇总
- STS1 Boss 总数：**28**
- STS2 本体直接保留：**12**
- STS2 本体等价替代（重命名/重设定）：**2**
- STS2 本体缺失：**14**
- 这 14 个里，ClassicMode 已实现：**7**
- 这 14 个里，ClassicMode 仍未实现：**9**

### 6.2 STS2 本体直接保留（12）
- `Astrolabe`
- `BlackBlood`
- `BlackStar`
- `CallingBell`
- `Ectoplasm`
- `EmptyCage`
- `PandorasBox`
- `PhilosophersStone`
- `RunicPyramid`
- `SneckoEye`
- `Sozu`
- `VelvetChoker`

池分布（STS2）：上述基本都在 `EventRelicPool`（而非独立“Boss池”命名）。

### 6.3 STS2 本体等价替代（2）
- `Frozen Core` -> `InfusedCore`
- `Ring of the Serpent` -> `RingOfTheDrake`

说明：本 Mod 在“仅经典遗物”模式下已改 `TouchOfOrobas` 升级映射，回到 STS1 版本。

### 6.4 STS2 缺失但 ClassicMode 已实现（7）
- `Frozen Core`
- `Ring of the Serpent`
- `Hovering Kite`
- `Inserter`
- `Nuclear Battery`
- `Mark of Pain`
- `Wrist Blade`

当前池分布（ClassicMode 代码现状）：
- 已进入经典角色池：
  - `RingOfTheSerpent` -> `ClassicSilentRelicPool`（Starter）
  - `FrozenCore` -> `ClassicDefectRelicPool`（Starter）
  - `BlackBlood`（STS2本体）-> `ClassicIroncladRelicPool`（Starter upgrade）
- 仅有实现类、暂未放入任何 Classic 池：
  - `HoveringKite` / `Inserter` / `NuclearBattery` / `MarkOfPain` / `WristBlade`

### 6.5 STS2 缺失且 ClassicMode 未实现（9）
- `Busted Crown`
- `Coffee Dripper`
- `Cursed Key`
- `Fusion Hammer`
- `Runic Cube`
- `Runic Dome`
- `Sacred Bark`
- `Slaver's Collar`
- `Tiny House`

## 7. 后续建议（Boss 遗物）
- 先统一“Boss 在 STS2 中的承载方式”：
  - 选项 A：沿用 STS2 `EventRelicPool` 思路；
  - 选项 B：在经典模式内通过角色池/事件入口显式投放。
- 把“已实现但未入池”的 5 个先补入可获得路径，再做数值与稀有度校准。
