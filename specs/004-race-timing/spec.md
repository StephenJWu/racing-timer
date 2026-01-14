# Feature Specification: 比赛计时功能

**Feature Branch**: `004-race-timing`  
**Created**: 2025-01-27  
**Updated**: 2025-01-27  
**Status**: Draft  
**Input**: User description: "实现比赛计时功能，包括：实时计时（毫秒级精度）、比赛状态管理（待开始/待发令/比赛中/已完成）、设备扫描集成（支持模拟和真实设备）、计时结果显示、多组并发计时支持，先实现MVP功能，后续集成硬件后再完整实现"

**详细需求补充**: 页面布局（左右分栏）、比赛分组管理（添加分组、分组卡片）、人员计时面板（表格显示、实时计时、状态更新）、设备扫描自动完成、多组并发计时控制

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 比赛分组添加和管理 (Priority: P1)

用户需要能够添加和管理比赛分组，系统应该根据"人员分组"功能预先分配的信息自动填充芯片组和比赛圈数，并允许用户在比赛时重新选择。

**Why this priority**: 比赛分组是计时功能的基础，用户需要先创建分组才能开始计时。分组管理功能必须能够正确关联到参赛人员和预设的芯片组、比赛参数。

**Independent Test**: 可以通过选择学校、年级、班级、组别，验证系统是否正确从"人员分组"功能获取预设的芯片组和比赛圈数，并允许用户修改后提交来测试。

**Acceptance Scenarios**:

1. **Given** 用户已打开比赛计时页面, **When** 用户点击"添加比赛分组"按钮, **Then** 应该打开新增比赛分组弹窗，显示学校、年级、班级、组别下拉框，芯片组和比赛圈数默认不可选择
2. **Given** 用户在弹窗中依次选择学校、年级、班级、分组, **When** 选择完成后, **Then** 系统应该根据"人员分组"功能里为比赛组预先分配好的芯片组和比赛参数，反显到页面上（芯片组显示"颜色块-芯片组名"，比赛圈数显示预设值）
3. **Given** 系统已反显预设的芯片组和比赛圈数, **When** 用户需要修改, **Then** 用户可以根据需要重新选择芯片组和比赛圈数（芯片组显示为"颜色块-芯片组名"的长方形色块，比赛圈数显示1到20的数字供选择）
4. **Given** 用户已选择所有选项, **When** 用户点击确认按钮, **Then** 系统应该检查所有选项和输入项都全部完成，如果选择的芯片组和比赛圈数同预先分配的不一致，需要为该人员分组重新分配芯片标签号码和芯片内部号码，以及比赛组的颜色
5. **Given** 用户提交分组成功, **When** 系统创建分组, **Then** 应该在左侧分组卡片区创建一个新的分组卡片，卡片背景色为所选芯片组的颜色
6. **Given** 用户需要清空所有分组, **When** 用户点击"清空分组列表"按钮, **Then** 系统应该提示确认，确认后删除所有分组卡片

---

### User Story 2 - 分组卡片显示和交互 (Priority: P1)

用户需要能够通过分组卡片查看和管理每个比赛分组的状态和信息，卡片应该清晰显示分组信息、比赛状态、参赛人数、比赛圈数，并提供操作按钮。

**Why this priority**: 分组卡片是用户管理多个比赛分组的主要界面，用户需要通过卡片快速了解每个分组的状态并进行操作。这是多组并发计时的基础。

**Independent Test**: 可以通过创建多个分组，验证卡片是否正确显示信息，点击卡片后右侧面板是否正确显示对应分组的人员，操作按钮是否正确响应来测试。

**Acceptance Scenarios**:

1. **Given** 用户已创建比赛分组, **When** 分组卡片显示, **Then** 卡片应该显示上中下结构：上部左侧为"学校-年级-班级-组名"，右侧为当前分组的比赛状态标签（待开始/待发令/比赛中/已完成），中部左侧为当前分组的参赛人数，右侧为分组的比赛圈数，下部为操作按钮区
2. **Given** 分组卡片已显示, **When** 用户点击卡片, **Then** 右侧人员计时面板应该显示当前选中分组的所有参赛人员
3. **Given** 分组状态为"待开始", **When** 用户查看卡片, **Then** 卡片下部应该显示"等待发令"按钮（左侧）和"删除分组"按钮（右侧）
4. **Given** 用户点击"等待发令"按钮, **When** 按钮被点击, **Then** 应该显示"开跑"和"违规重跑"两个按钮，分组状态更新为"待发令"
5. **Given** 用户点击"开跑"按钮, **When** 按钮被点击, **Then** "开跑"按钮应该变成"违规重跑"按钮，按钮颜色从绿色变为橙色，分组状态更新为"比赛中"，并且人员计时面板里每个参赛人员的计时和状态栏的计时时间和圈数都会开始变动
6. **Given** 用户点击"违规重跑"按钮, **When** 按钮被点击, **Then** 人员计时面板应该回到初始化状态（计时清零），按钮也回到"等待发令"、"删除分组"两个按钮，分组状态重置为"待开始"
7. **Given** 用户点击"删除分组"按钮, **When** 按钮被点击, **Then** 应该提示确认是否删除当前分组，确认后删除该分组卡片
8. **Given** 多个分组卡片同时显示, **When** 用户查看, **Then** 每个组的比赛计时都是独立的，只有选择当前分组的卡片，人员计时面板才显示当前比赛的分组

---

### User Story 3 - 人员计时面板显示和实时更新 (Priority: P1)

用户需要能够在人员计时面板中查看当前选中分组的所有参赛人员，实时查看每个人员的计时状态和完成情况，系统应该实时更新计时显示（毫秒级精度）。

**Why this priority**: 人员计时面板是计时功能的核心界面，用户需要实时查看每个参赛人员的计时状态。这是计时功能的核心价值体现。

**Independent Test**: 可以通过选择分组，验证表格是否正确显示该分组的所有参赛人员，点击"开跑"后验证计时是否正确更新，完成人员是否正确标记为绿色来测试。

**Acceptance Scenarios**:

1. **Given** 用户已选择比赛分组, **When** 人员计时面板显示, **Then** 应该显示表格，包含列：组内序号、年级、班级、姓名、性别、准考证号、芯片标签号码、计时、状态
2. **Given** 计时未开始, **When** 用户查看计时列, **Then** 计时默认显示"00:00:00:000"，精度到毫秒
3. **Given** 用户点击分组卡片里的"等待发令"按钮, **When** 按钮被点击, **Then** 人员计时面板应该进入计时准备状态，但计时仍显示"00:00:00:000"
4. **Given** 用户点击分组卡片里的"开跑"按钮, **When** 按钮被点击, **Then** 计时器应该开始运行，每个参赛人员的计时列和状态栏的计时时间、圈数都应该实时更新（每10-50ms更新一次）
5. **Given** 计时正在进行中, **When** 设备扫描检测到该组人员扫描次数达到预设次数, **Then** 系统应该自动标记该人员为完成，更新该行的背景为绿色，更新卡片里面的分组状态为"已完成"（如果所有人员都完成），状态标签也更新为绿色
6. **Given** 比赛完成人员, **When** 人员完成, **Then** 该行应该变为绿色背景，计时停止更新，状态显示为"已完成"
7. **Given** 未完成人员, **When** 比赛进行中, **Then** 该行应该保持原色，计时继续更新，状态显示为"比赛中"

---

### User Story 4 - 多组并发计时控制 (Priority: P1)

用户需要能够同时管理多个比赛分组，每个分组的计时应该独立运行，系统应该支持多个分组同时处于不同状态，并确保分组间隔合理。

**Why this priority**: 在实际比赛中，经常需要同时进行多组比赛，系统必须支持多组并发计时。这是实际使用中的核心需求。

**Independent Test**: 可以通过创建多个分组，同时让它们进入不同状态（待发令、比赛中），验证每个分组的计时是否独立运行，开跑时是否检查分组间隔来测试。

**Acceptance Scenarios**:

1. **Given** 用户已创建多个比赛分组, **When** 用户查看分组卡片区, **Then** 应该支持同时显示多个组的卡片，每个组的比赛计时都是独立的
2. **Given** 多个分组同时存在, **When** 用户选择不同分组卡片, **Then** 人员计时面板应该只显示当前选中分组的人员，其他分组的人员不显示
3. **Given** 支持多个组同时处于"等待发令"状态, **When** 多个分组都进入待发令状态, **Then** 每个分组的操作按钮应该独立显示，互不影响
4. **Given** 用户需要让多个分组同时比赛, **When** 用户点击多个分组的"开跑"按钮, **Then** 系统应该检查分组的间隔，确保分组间隔最好大于10秒，不同分组不要同时开始（如果间隔小于10秒，应该提示用户）
5. **Given** 多个分组同时处于"比赛中"状态, **When** 计时进行中, **Then** 每个分组的计时应该独立运行，互不干扰，每个分组的人员计时面板独立更新
6. **Given** 多个分组同时比赛, **When** 设备扫描事件到达, **Then** 系统应该能够正确识别扫描事件属于哪个分组，并更新对应分组的人员计时

---

### User Story 5 - 设备扫描自动完成检测 (Priority: P2)

用户需要系统能够通过设备扫描自动检测参赛人员完成情况，当扫描次数达到预设的比赛圈数时，自动标记人员为完成。

**Why this priority**: 设备扫描自动完成是比赛计时系统的核心特性，能够大大提高计时效率和准确性。虽然MVP阶段可以先手动标记完成，但设备扫描自动完成是完整功能的重要组成部分。

**Independent Test**: 可以通过模拟设备扫描事件，验证系统是否能够正确识别扫描的芯片标签号码，关联到对应参赛人员，累计扫描次数，达到预设圈数时自动标记完成来测试。

**Acceptance Scenarios**:

1. **Given** 比赛正在进行中, **When** 设备扫描到参赛人员的芯片标签号码, **Then** 系统应该识别该芯片标签号码，关联到对应的参赛人员，累计该人员的扫描次数
2. **Given** 参赛人员的扫描次数, **When** 扫描次数达到预设的比赛圈数, **Then** 系统应该自动标记该人员为完成，更新该行的背景为绿色，停止该人员的计时更新
3. **Given** 分组内所有参赛人员, **When** 所有人员都完成, **Then** 系统应该更新分组卡片的状态为"已完成"，状态标签也更新为绿色
4. **Given** 设备扫描到未注册的芯片标签号码, **When** 扫描事件到达, **Then** 系统应该显示错误提示，允许用户手动关联到参赛人员或忽略该扫描
5. **Given** 设备扫描事件, **When** 扫描事件到达, **Then** 系统应该在50ms内处理扫描事件，更新对应人员的状态和计时

---

### User Story 6 - 计时结果保存和历史记录 (Priority: P2)

用户需要系统能够保存每次比赛的计时结果，包括每个参赛人员的完成时间、圈数、状态等信息，并能够查看历史记录。

**Why this priority**: 计时结果的保存是比赛计时系统的重要功能，用户需要能够查看历史记录、导出数据、进行数据分析等。虽然优先级低于实时计时，但这是功能完整性的重要组成部分。

**Independent Test**: 可以通过完成一次比赛，验证计时结果是否正确保存到数据库，包括参赛人员信息、完成时间、圈数等，然后查看历史记录列表来测试。

**Acceptance Scenarios**:

1. **Given** 比赛已完成, **When** 所有参赛人员都完成或用户手动结束比赛, **Then** 系统应该保存计时结果到数据库，包括每个参赛人员的完成时间、圈数、状态等信息
2. **Given** 用户需要查看历史记录, **When** 用户查看计时结果列表, **Then** 应该按时间倒序显示所有比赛记录，包括分组信息、参赛人数、完成人数、比赛时间等
3. **Given** 历史记录列表, **When** 用户需要筛选, **Then** 应该支持按日期、分组、状态等条件筛选
4. **Given** 用户需要查看详细信息, **When** 用户点击某条历史记录, **Then** 应该显示该次比赛的详细信息，包括所有参赛人员的完成情况

---

### Edge Cases

- **分组间隔冲突**：如果用户尝试让两个分组同时开跑（间隔小于10秒），系统应该如何处理？应该提示用户并阻止同时开跑，或要求用户确认
- **芯片组和比赛圈数不一致**：如果用户修改了预设的芯片组和比赛圈数，系统如何重新分配芯片标签号码和芯片内部号码？需要调用"人员分组"功能的重新分配逻辑
- **设备扫描冲突**：如果设备扫描到未注册的芯片标签号码，系统应该如何处理？应该显示错误提示，允许用户手动关联或忽略
- **计时中断**：如果应用程序在计时过程中意外关闭，系统应该如何处理？应该保存未完成的计时记录，允许用户恢复或取消
- **多组并发管理**：如果同时运行多个分组，用户如何快速识别当前操作的是哪个分组？应该通过卡片选中状态和人员面板标题清晰标识
- **扫描次数异常**：如果设备扫描次数超过预设圈数，系统应该如何处理？应该记录异常扫描，允许用户手动调整
- **分组删除冲突**：如果用户尝试删除正在比赛中的分组，系统应该如何处理？应该提示用户先停止比赛，或强制结束比赛后删除

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a two-panel layout: left panel for race group management, right panel for participant timing display
- **FR-002**: System MUST provide "Add Race Group" button that opens a dialog for creating new race groups
- **FR-003**: System MUST allow users to select School, Grade, Class, and Group in the add group dialog via dropdowns
- **FR-004**: System MUST automatically retrieve and display pre-assigned ChipGroup and RaceLaps from "Personnel Grouping" feature based on selected School/Grade/Class/Group
- **FR-005**: System MUST allow users to modify ChipGroup and RaceLaps in the add group dialog (ChipGroup displayed as "color block - chip group name", RaceLaps from 1 to 20)
- **FR-006**: System MUST validate that all required fields are filled before submitting the add group dialog
- **FR-007**: System MUST re-assign chip label numbers and chip internal numbers, as well as group color, if user modifies ChipGroup or RaceLaps from pre-assigned values
- **FR-008**: System MUST create a group card in the left panel after successfully adding a race group, with background color matching the selected ChipGroup color
- **FR-009**: System MUST provide "Clear All Groups" button that removes all group cards after user confirmation
- **FR-010**: System MUST display group cards with three sections: top (School-Grade-Class-Group name and status badge), middle (participant count and race laps), bottom (action buttons)
- **FR-011**: System MUST display group status badges with four states: 待开始 (Pending), 待发令 (Ready), 比赛中 (In Progress), 已完成 (Completed)
- **FR-012**: System MUST allow users to click group cards to select a group and display its participants in the right panel
- **FR-013**: System MUST provide "等待发令" (Wait for Start) button that shows "开跑" (Start) and "违规重跑" (Illegal Restart) buttons when clicked
- **FR-014**: System MUST provide "开跑" (Start) button that changes to "违规重跑" (Illegal Restart) button (color changes from green to orange) and starts timing for all participants in the group
- **FR-015**: System MUST provide "违规重跑" (Illegal Restart) button that resets timing panel to initial state and resets buttons to "等待发令" and "删除分组"
- **FR-016**: System MUST provide "删除分组" (Delete Group) button that removes the group card after user confirmation
- **FR-017**: System MUST display participant timing panel as a table with columns: Group Sequence, Grade, Class, Name, Gender, Exam Number, Chip Label Number, Timing, Status
- **FR-018**: System MUST display timing in format "00:00:00:000" (hours:minutes:seconds:milliseconds) with millisecond precision
- **FR-019**: System MUST update timing display every 10-50ms when timing is in progress
- **FR-020**: System MUST mark completed participants with green background in the timing panel
- **FR-021**: System MUST keep incomplete participants with original background color during race
- **FR-022**: System MUST support multiple groups running concurrently, with each group's timing independent
- **FR-023**: System MUST check group start interval (minimum 10 seconds) when multiple groups start simultaneously
- **FR-024**: System MUST prevent simultaneous start of different groups (or require user confirmation if interval is less than 10 seconds)
- **FR-025**: System MUST allow multiple groups to be in "等待发令" (Ready) state simultaneously
- **FR-026**: System MUST automatically detect participant completion when device scan count reaches preset race laps
- **FR-027**: System MUST update group status to "已完成" (Completed) and status badge to green when all participants in the group are completed
- **FR-028**: System MUST associate device scans (chip label numbers) with participants and track scan count per participant
- **FR-029**: System MUST handle unregistered chip label numbers in device scans with error prompt and manual association option
- **FR-030**: System MUST process device scan events within 50ms of receipt
- **FR-031**: System MUST persist timing results to database including participant completion time, laps, status for each race group
- **FR-032**: System MUST provide timing result history view with filtering by date, group, and status
- **FR-033**: System MUST integrate with "Personnel Grouping" feature to retrieve pre-assigned ChipGroup, RaceLaps, chip label numbers, and chip internal numbers [NEEDS CLARIFICATION: API contract for integration not fully specified]
- **FR-034**: System MUST support timer result export functionality (CSV/Excel format) [NEEDS CLARIFICATION: export format and fields not fully specified]

### Key Entities *(include if feature involves data)*

- **RaceGroup**: Represents a race group with school, grade, class, group name, chip group, race laps, status, and participant list. Key attributes: Id, School, Grade, Class, GroupName, ChipGroupId, ChipGroupName, ChipGroupColor, RaceLaps, Status (待开始/待发令/比赛中/已完成), ParticipantCount, CreatedAt, UpdatedAt
- **RaceTiming**: Represents timing information for a single participant in a race group. Key attributes: Id, RaceGroupId, ParticipantId, ChipLabelNumber, StartTime, CurrentTime, ElapsedMilliseconds, ScanCount, TargetLaps, Status (比赛中/已完成), IsCompleted, CompletedAt
- **RaceResult**: Represents a completed race result with participant completion details. Key attributes: Id, RaceGroupId, ParticipantId, CompletionTime, ElapsedMilliseconds, FormattedTime, ScanCount, LapsCompleted, Status, CreatedAt
- **DeviceScanEvent**: Represents a device scan event for automatic completion detection. Key attributes: Id, RaceGroupId, ChipLabelNumber, ScanTime, EventType, Processed
- **Participant**: Existing entity, used for race group association. Key attributes: Id, Name, BibNumber, ChipNumber, ChipLabelNumber, GroupName, School, Grade, Class

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Timer accuracy MUST be within ±10ms for timing operations up to 1 hour duration
- **SC-002**: Timer display MUST update at least every 50ms to provide smooth visual feedback
- **SC-003**: System MUST support at least 10 concurrent race groups without performance degradation
- **SC-004**: Group card creation MUST complete within 200ms after user submits add group dialog
- **SC-005**: System MUST load and display participant timing panel within 300ms after group card selection
- **SC-006**: System MUST process device scan events within 50ms of receipt
- **SC-007**: System MUST correctly associate 95%+ of device scans with participants via chip label numbers
- **SC-008**: System MUST automatically detect participant completion within 100ms of final scan event
- **SC-009**: System MUST maintain timer accuracy even when application is minimized or system is under load
- **SC-010**: System MUST validate group start interval (minimum 10 seconds) with 100% accuracy
- **SC-011**: System MUST persist race results to database within 500ms after race completion
- **SC-012**: System MUST load and display race result history within 500ms for up to 1000 records
