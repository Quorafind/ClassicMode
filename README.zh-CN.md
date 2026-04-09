# Classic Mode —— 杀戮尖塔 2 模组

让《杀戮尖塔 2》的角色使用 **《杀戮尖塔 1》** 的卡池、遗物池，以及经典 Boss
内容。经典卡牌、经典遗物、经典 Boss 三项开关可在角色选择界面独立切换。

> English: see [README.md](README.md)

## 构建所需

想要自己编译这个模组，需要准备以下环境：

1. **《杀戮尖塔 2》本体** —— 编译需要引用游戏自带的 DLL，位于
   `<STS2 安装目录>\data_sts2_windows_x86_64\`。
2. **.NET 9 SDK** —— `dotnet --version` 应返回 `9.x`。
3. **Python 3.9+**，并安装了 `Pillow`（`pip install pillow`）。
4. **Godot 4.5+（Mono 版本）** —— 仅当你希望模组能在 Android 上运行时需要；
   桌面端构建时如果找不到 Godot，脚本会自动跳过贴图导入步骤，不会报错。
5. **一份解包后的《杀戮尖塔 1》资源目录**，详见下一节。

### 如何解包《杀戮尖塔 1》资源

ClassicMode 复用了 STS1 的卡牌立绘、遗物图标，以及中英文本地化。本仓库
**不会包含任何 STS1 素材**，需要你用自己合法持有的 STS1 自行解包。

大致流程（示例，只要最终目录结构符合即可）：

1. 准备一个 JAR 解压工具，例如
   [jd-cli](https://github.com/intoolswetrust/jd-cli)，或者直接用任意解压软件
   打开 Steam 目录下的 `SlayTheSpire.jar`。
2. 把内容解压到任意目录，使最终结构类似：
   ```
   SlayTheSpire_unpacked/
     images/
       1024Portraits/red/attack/*.png
       1024Portraits/green/...
       1024Portraits/blue/...
       relics/*.png
     localization/
       eng/cards.json
       eng/relics.json
       zhs/cards.json
       zhs/relics.json
   ```
3. 在构建时通过 `-Sts1Dir` 参数、或 `STS1_UNPACKED_DIR` 环境变量告诉构建脚本
   这个目录的位置（见下文）。

如果不提供这个目录，构建会直接失败并给出提示。如果你只是改 C# 代码、不需要
重新生成素材，可以加 `-SkipAssets` 跳过整个 `prepare_assets.py` 步骤，复用
上次构建得到的 `_pck_src`。

## 构建方法

在仓库根目录打开 PowerShell，任选一种方式：

```powershell
# 方式 A：使用环境变量（推荐，重复构建最方便）
$env:STS2_GAME_DIR     = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
$env:STS1_UNPACKED_DIR = "C:\path\to\SlayTheSpire_unpacked"
.\build.ps1

# 方式 B：命令行参数
.\build.ps1 `
  -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2" `
  -Sts1Dir "C:\path\to\SlayTheSpire_unpacked"

# 方式 C：只编译 C#，跳过素材准备
.\build.ps1 -SkipAssets
```

`build.ps1` 会自动尝试几个常见的 Steam 安装目录，因此多数情况下首次运行时
只需要指定 `-Sts1Dir` 就能跑起来。

## 构建产物

成功构建后，所有产物都会生成到仓库根下的 `build/`：

```
build/
  ClassicMode/
    ClassicMode.dll
    ClassicMode.json        # 0.99+ 使用的清单文件
    ClassicMode.pck         # 打包好的资源
  ClassicMode-STS2_0.98.x-<version>.zip
  ClassicMode-STS2_0.99-<version>.zip
```

安装方法：把 `build\ClassicMode\` 整个目录丢进游戏的 `mods\` 文件夹即可，
也可以直接分发上面的 zip 压缩包。

## 仓库目录结构

```
ClassicMode/
├── Base/                 # 卡牌 / 遗物基类
├── Cards/                # 铁甲 / 静默 / 缺陷三个角色的卡牌定义
├── Encounters/           # 经典 Boss（六头鬼等）
├── Monsters/             # 经典怪物模型
├── Patches/              # Harmony 补丁
├── Pools/                # 卡池 / 遗物池定义
├── Powers/               # 经典能力模型
├── Relics/               # 经典遗物定义
├── assets/               # 本地化 + 手动随仓库发布的图像
├── scripts/               # 构建时用到的脚本（见下）
│   ├── mod_build_common.ps1
│   ├── pack_godot_pck.py
│   └── import_assets.py
├── ClassicBootstrap.cs   # 模组入口 + Harmony 启动器
├── ClassicConfig.cs      # 用户开关的持久化
├── ClassicMode.csproj    # 项目文件（会读取 STS2_GAME_DIR）
├── prepare_assets.py     # 将 STS1 解包资源转换成 _pck_src 内容
├── build.ps1             # 独立仓库的构建入口
├── mod_manifest.json
├── LICENSE               # MIT
└── README.md / README.zh-CN.md
```

### `scripts/` 目录说明

`scripts/` 里是构建过程中需要的 PowerShell / Python 辅助脚本，负责把源代码
变成一个打包好的 `.pck` 文件：

- **`mod_build_common.ps1`** —— 可复用的 PowerShell 函数：查找 Godot、运行
  无头贴图导入、打包 PCK、生成发布用 zip。
- **`pack_godot_pck.py`** —— 一个纯 Python 实现的最小 Godot 4 PCK 打包器。
- **`import_assets.py`** —— 在 Godot 无头导入之后运行，把 `.ctex` 文件从
  `.godot/imported/` 重新放到模组自己的目录下，避免和宿主游戏的 `.godot/`
  冲突。

这些脚本完全不依赖仓库之外的任何文件，可以直接在仓库内修改。

## 许可证

本仓库使用 [MIT](LICENSE) 许可证。

ClassicMode 不附带任何《杀戮尖塔》/《杀戮尖塔 2》的素材文件 —— 仅在编译期
引用 STS2 的 DLL，在生成资源时才会读取你本地解包的 STS1 目录。
