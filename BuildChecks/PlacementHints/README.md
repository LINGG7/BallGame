# 激励广告辅助放置

## 接入关卡（编辑器配置）

1. 游戏启动时，GameManager 自动将提示功能绑定到 GameHud 现有的 **Top Bar/Help** 按钮，自动补齐缺失的提示控制器。无需逐关运行设置菜单，也不创建新按钮。
2. 选择 GameManager 的 Level 资产，在 **Placement Hints** 中按顺序填写目标。
   - `tool`：与库存和已放置道具使用同一个 ToolDefinition。
   - `position`、`rotationDegrees`：道具根物体的世界位置、Z 轴角度。
   - `snapRadius`：松手吸附距离，默认 0.65 世界单位。
   - `cameraPosition`：相机要观察的世界 XY 坐标；建议先与目标位置一致。
   - `cameraMoveDuration`：镜头移动时长，默认 0.6 秒；0 表示直接聚焦。
   - `positionTolerance` / `rotationTolerance`：判定已经正确放置的容差，默认 0.05 单位 / 2 度。
3. 也可以在场景或 Play Mode 中选中一个已经摆好的 PlaceableTool，执行 **Append Selected Tool As Target**。每次追加一个目标，保存 Level 资产；录入时使用道具默认尺寸，位置和旋转会记录，缩放不会记录。

目前没有答案位置数据，提示列表不预填猜测数据。配置至少一个有效目标后，现有 Help 按钮即可用于测试。Set Up Current Scene 菜单已移除；Append Selected Tool As Target 菜单继续保留。

## 模拟广告与正式接入

- 自动绑定优先保留控制器已有的广告 Provider；没有指定时复用同物体上的 Provider，再按构建环境补齐。Unity Editor / Development Build 默认添加 MockHintAdProvider，正式 Player 默认添加 LegacyHintAdAdapter。
- 默认 1 秒后返回 Rewarded；可在 Inspector 修改结果为 Cancelled / Failed。
- 开启 Manual Completion 后，使用组件右键菜单 Complete Mock 的三个入口手动回调。
- 正式 Player 中模拟广告不可用，不会默认发放奖励。
- 真实 SDK 可继承 HintRewardedAdProvider，实现 IsAvailable / Show，必须在 Unity 主线程确认奖励后回调 HintAdResult.Rewarded。取消和失败分别返回对应结果。
- LegacyHintAdAdapter 可复用现有 BallSkinService.RewardedAdService；旧接口只有 bool，false 映射为 Failed。替换控制器的 Ad Provider 引用即可。
- 120 秒无回调会恢复按钮；重复和迟到回调被忽略。重开、重新加载或禁用控制器会清理提示。

## 行为

- 按顺序查找未正确放置、且存在可用库存或可移动匹配实例的目标。
- 一次广告只开启一个提示；错误道具、范围外松手或目标被障碍物占据不会消耗提示。
- 精灵虚影没有碰撞、物理和道具脚本。新放置和已有道具移动均复用原有碰撞校验和撤销流程。
- 吸附仅对齐位置与旋转，成功后仍能移动。再次移走或撤销后，该位置可再次成为广告提示目标。
- 广告等待与镜头移动时阻止道具操作；镜头完成后恢复操作并停留。
- 模拟运行时隐藏未完成虚影；返回建造（包括自动失败重置）清除提示，不跨重开保存。
- 预设点应由设计者验证可以正常放置。特定道具尺寸变化或自定义渲染需要额外适配；当前虚影针对现有 SpriteRenderer 道具。

## 验证

运行 `pwsh -File BuildChecks/PlacementHints/Validate.ps1` 使用本机 Unity 编译器验证全部运行时代码、非开发 Player 分支及新增编辑器/测试代码。输出仅写入本目录，不启动 Unity、不创建临时 Unity 项目、不清理目录。

Unity Test Runner 的 EditMode 中运行 **PlacementHintTests** 和已有 **PlacementHistoryTests**。新增测试覆盖：奖励去重、取消/失败、重开与迟到回调、无效配置、顺序跳过、错误道具、范围外松手、碰撞拒绝、库存撤销、旋转吸附撤销、相机与虚影、输入锁、超时和禁用清理。

本次已执行编译检查。编辑器自动操作工具无法启动，因此尚未执行 Unity 行为测试或场景视觉验收；需要在 Unity 中运行测试并在配置后的关卡检查 Help 按钮、虚影显示与镜头移动。
