# 一次广告开启一组辅助放置

## 配置与旧数据

在关卡 GameManager 的 LevelDefinition 资产中编辑 **Placement Hint Groups**：每项填写组名 `groupName`，在 `targets` 中按引导顺序添加多个目标。目标继续配置 ToolDefinition、世界位置、Z 轴角度、吸附半径、镜头位置/移动时长和正确放置容差。

已有 `placementHints` 数据不会被自动改写。没有新组配置时，每个旧目标仍独立算一组，维持原广告次数。Inspector 的 **转换旧配置为分组** 会完整复制旧参数，每项生成一个组；随后可手动合并、调整顺序。转换支持 Undo，保存资产后生效。

新组非空时只使用新组配置。清空全部新组会重新使用保留的旧数据；若想彻底关闭旧提示，应明确调整旧数据，而不是误以为它已被删除。

## 多选录入

1. 在 Hierarchy 中选中一个或多个场景道具，也可选择其子物体。
2. 执行 **Ball Game → Placement Hints → Append Selected Tool As Target**。
3. 在窗口中选择已有组，或输入组名新建组，点击“记录目标”。

录入按 Hierarchy 顺序进行；同一道具的重复选择会合并，同组内相同道具类型、位置和角度的目标会跳过。位置以打开窗口时为准，不记录缩放。Fan 等未填 Definition 的预制体实例会通过预制体关联查找 ToolDefinition；匹配不唯一时需要明确指定。

首次录入新分组时，会先把旧目标按每项一组保留，录入与转换作为一次可撤销编辑。菜单只修改所选关卡定义；不修改场景道具或预制体。

## 游戏流程

- 游戏自动绑定现有 GameHud/Top Bar/Help；按钮原文字不改变。
- 一次成功广告开启首个未完成组。组内依次显示单个虚影、移动镜头，无需额外广告。
- 当前目标仍未完成时保持引导；完成后选择本组首个未完成目标，自动跳过已正确放置项。
- 本组所有目标必须同时满足位置和旋转要求才结束。重复类型道具采用最大一对一匹配，一个实例不能满足多个目标。
- 移走、旋转、删除或撤销完成项后，会重新纳入本组引导；不会在拖动中途切换目标或抢镜头。
- 完成整组后再移动道具，需要重新看广告才能再次开启该组。
- 镜头移动时暂时锁定摆放，移动结束后恢复操作。沿用当前虚影呼吸光和上方操作文字。
- 空组、错误配置或道具总数量不足时，不播放广告，状态栏显示原因。预设位置还需设计者确认能通过正常碰撞校验。

## 试运行与重开

试运行期间隐藏虚影，不更新完成状态。ResetToBuild（包括失败后返回摆放）保留广告获得的组，在恢复道具和镜头后继续引导。

RetryLevelAndClearTools、重新载入关卡、离开场景或禁用控制器清除本次辅助。没有跨关卡或应用重启存档。

## 广告

已绑定 Provider 优先复用；Editor / Development Build 默认使用 MockHintAdProvider，正式 Player 默认使用 LegacyHintAdAdapter。模拟 Provider 在正式构建中不可用。

Mock 默认一秒成功，可选择 Cancelled / Failed；开启 Manual Completion 后使用组件右键菜单完成回调。真实 SDK 适配必须在 Unity 主线程确认奖励后回调。重复、迟到、超时与重开后的回调受版本检查保护。

## 验证

- `pwsh -File BuildChecks/PlacementHints/Validate.ps1`：运行时、正式构建分支、编辑器工具与隔离测试程序集编译。
- `dotnet run --project BuildChecks/PlacementHints/MatchingChecks/MatchingChecks.csproj`：编译真实 PlacementHintGroup 源码，使用边界替身，将 3000 个匹配图与穷举结果比较，并检查数量/配置验证。已通过 16669 个断言。此项不代替 Unity 物理或场景测试。
- Unity 菜单 **Ball Game → Placement Hints → Run Placement Tests**：执行 PlacementHintTests 和 PlacementHistoryTests，结果写入 `BuildChecks/PlacementHints/unity-tests.log`。也可在 Test Runner 中运行这两个 EditMode 测试类。
- 新增回归覆盖整组连续放置、撤销/移走后的重访、最大一对一匹配、库存不足、试运行恢复、真正重开、旧配置转换及多选 Fan 录入。

当前编译和独立算法检查通过；原项目 Unity 编辑器尚未导入新脚本，自动操作工具无法启动，因此 Unity 行为测试和场景视觉验收尚未完成。待 Unity 完成编译后运行上述菜单，再检查连续镜头引导、光效和文字位置。
