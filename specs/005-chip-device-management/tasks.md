# Tasks: 芯片设备管理

**Input**: Design documents from `/specs/005-chip-device-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories)

**Tests**: Per Constitution (Testing & Debugging Standards), all core logic MUST have unit tests with >80% coverage. Test tasks are included below and MUST be implemented. Data access operations MUST be abstracted through interfaces for testability.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `Timer/Timer/` at repository root
- All paths are relative to `Timer/Timer/` directory

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and dependencies verification

**Note**: Since this is an extension to existing project, dependencies (Microsoft.Data.Sqlite, ClosedXML, CommunityToolkit.Mvvm) are already integrated. This phase mainly verifies prerequisites.

- [ ] T001 Verify Microsoft.Data.Sqlite NuGet package is installed in Timer/Timer/Timer.csproj
- [ ] T002 Verify ClosedXML NuGet package is installed in Timer/Timer/Timer.csproj
- [ ] T003 Verify CommunityToolkit.Mvvm NuGet package is installed in Timer/Timer/Timer.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 [P] Create ChipGroup model in Timer/Timer/Models/ChipGroup.cs with properties (Id, GroupName, Color, CreatedAt, UpdatedAt, ChipCount as computed property)
- [x] T005 [P] Create Chip model in Timer/Timer/Models/Chip.cs with properties (Id, ChipGroupId, LabelNumber, InternalNumber, CreatedAt, UpdatedAt)
- [x] T006 Create IChipRepository interface in Timer/Timer/Services/IChipRepository.cs with all required methods (GetAllChipGroupsAsync, GetChipGroupByIdAsync, GetChipsByGroupIdAsync, AddChipGroupAsync, UpdateChipGroupAsync, AddChipsAsync, UpdateChipAsync, DeleteChipGroupAsync, DeleteChipAsync, GetChipCountByGroupIdAsync, ExistsByLabelNumberAsync, transaction methods)
- [x] T007 Update DatabaseContext.CreateTablesAsync in Timer/Timer/Data/DatabaseContext.cs to add ChipGroups and Chips table creation SQL with foreign key constraints and indexes

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Excel导入芯片信息 (Priority: P1) 🎯 MVP

**Goal**: 实现从Excel文件批量导入芯片信息，包括文件解析、数据校验、按组名分组、事务性保存和进度报告

**Independent Test**: 可以通过准备一个符合模板格式的Excel文件（包含序号、芯片标签号码、芯片内部编号、组号列），执行导入操作，验证数据是否正确解析、校验和保存到数据库来测试。导入成功后，应该能在芯片组列表中看到导入的芯片组和芯片详情。

### Tests for User Story 1 (REQUIRED per Constitution) ⚠️

> **NOTE: Per Constitution Testing Standards - Write these tests FIRST, ensure they FAIL before implementation. All core logic MUST achieve >80% coverage.**

- [ ] T008 [P] [US1] Create unit test for ChipImportService.ReadFromFileAsync in Timer/Timer.Tests/Services/ChipImportServiceTests.cs
- [ ] T009 [P] [US1] Create unit test for ChipImportService.ImportAsync in Timer/Timer.Tests/Services/ChipImportServiceTests.cs

### Implementation for User Story 1

- [x] T010 [P] [US1] Implement ChipRepository class in Timer/Timer/Services/ChipRepository.cs with IChipRepository interface, including transaction support methods
- [x] T011 [US1] Create IChipImportService interface in Timer/Timer/Services/IChipImportService.cs with ReadFromFileAsync and ImportAsync methods
- [x] T012 [US1] Create ChipImportService class in Timer/Timer/Services/ChipImportService.cs implementing IChipImportService, with Excel parsing (columns: 序号, 芯片标签号码, 芯片内部编号, 组号), data validation (non-empty fields, unique LabelNumber), grouping by group name, and transaction support
- [ ] T013 [US1] Update ChipViewModel in Timer/Timer/ViewModels/ChipViewModel.cs to add ImportCommand (AsyncRelayCommand), ImportProgress, ImportResult properties
- [ ] T014 [US1] Implement ImportExcelAsync method in Timer/Timer/ViewModels/ChipViewModel.cs with file selection, validation, and import logic
- [ ] T015 [US1] Update ChipView.xaml in Timer/Timer/Views/ChipView.xaml to add "导入芯片信息" button in upper section
- [ ] T016 [US1] Add import progress indicator and result display in Timer/Timer/Views/ChipView.xaml
- [ ] T017 [US1] Bind import button to ImportCommand in Timer/Timer/Views/ChipView.xaml

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently - Excel import should work with validation and transaction support

---

## Phase 4: User Story 2 - 芯片组列表显示和管理 (Priority: P1)

**Goal**: 实现芯片组列表表格显示，包括组名、组颜色（颜色块）、数量、操作列，支持选择芯片组

**Independent Test**: 可以通过导入一些测试数据，然后打开芯片设备页面，验证列表是否正确显示所有芯片组信息，选择芯片组后是否正确显示芯片详情来测试。

### Tests for User Story 2 (REQUIRED per Constitution) ⚠️

- [ ] T018 [P] [US2] Create unit test for ChipRepository.GetAllChipGroupsAsync in Timer/Timer.Tests/Services/ChipRepositoryTests.cs
- [ ] T019 [P] [US2] Create unit test for ChipRepository.GetChipCountByGroupIdAsync in Timer/Timer.Tests/Services/ChipRepositoryTests.cs

### Implementation for User Story 2

- [ ] T020 [US2] Update ChipViewModel in Timer/Timer/ViewModels/ChipViewModel.cs to add ChipGroups (ObservableCollection<ChipGroupViewModel>), SelectedChipGroup, IsLoading properties
- [ ] T021 [US2] Create ChipGroupViewModel display model in Timer/Timer/ViewModels/ChipGroupViewModel.cs (optional, can use ChipGroup directly with converter)
- [ ] T022 [US2] Implement LoadChipGroupsAsync method in Timer/Timer/ViewModels/ChipViewModel.cs to load chip groups with chip counts
- [ ] T023 [US2] Update ChipView.xaml in Timer/Timer/Views/ChipView.xaml to add DataGrid for chip groups in upper section with columns: selection (DataGrid selection), 组名, 组颜色 (DataGridTemplateColumn with Rectangle for color block), 数量, 操作 (Edit, Delete buttons)
- [ ] T024 [US2] Bind chip group DataGrid to ChipGroups collection and SelectedItem to SelectedChipGroup in Timer/Timer/Views/ChipView.xaml
- [ ] T025 [US2] Add loading indicator in Timer/Timer/Views/ChipView.xaml (bind to IsLoading)
- [ ] T026 [US2] Add empty state message in Timer/Timer/Views/ChipView.xaml (show when ChipGroups.Count == 0)
- [ ] T027 [US2] Call LoadChipGroupsAsync in ChipViewModel constructor or OnNavigatedTo method

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - import and list display should work correctly

---

## Phase 5: User Story 3 - 芯片详情查看 (Priority: P1)

**Goal**: 实现选中芯片组的芯片详情表格显示，包括芯片标签号码和芯片内部号码

**Independent Test**: 可以通过导入一些测试数据，选择一个芯片组，验证是否能够正确显示该芯片组的所有芯片详情来测试。

### Tests for User Story 3 (REQUIRED per Constitution) ⚠️

- [ ] T028 [P] [US3] Create unit test for ChipRepository.GetChipsByGroupIdAsync in Timer/Timer.Tests/Services/ChipRepositoryTests.cs

### Implementation for User Story 3

- [ ] T029 [US3] Update ChipViewModel in Timer/Timer/ViewModels/ChipViewModel.cs to add Chips (ObservableCollection<ChipViewModel>), implement SelectedChipGroup property change handler to load chips
- [ ] T030 [US3] Create ChipViewModel display model in Timer/Timer/ViewModels/ChipViewModel.cs (for chip details, can use Chip directly with converter)
- [ ] T031 [US3] Implement LoadChipsByGroupIdAsync method in Timer/Timer/ViewModels/ChipViewModel.cs to load chips for selected chip group
- [ ] T032 [US3] Update ChipView.xaml in Timer/Timer/Views/ChipView.xaml to add DataGrid for chip details in lower section with columns: 芯片标签号码, 芯片内部号码, 操作 (Edit, Delete buttons)
- [ ] T033 [US3] Bind chip details DataGrid to Chips collection in Timer/Timer/Views/ChipView.xaml
- [ ] T034 [US3] Add "请选择上方芯片组查看详情" message in Timer/Timer/Views/ChipView.xaml (show when SelectedChipGroup is null)

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work independently - import, list display, and chip details should work correctly

---

## Phase 6: User Story 4 - 芯片组编辑 (Priority: P2)

**Goal**: 实现芯片组信息编辑功能，包括组名和颜色设置，支持预定义颜色选择

**Independent Test**: 可以通过选择一个芯片组，点击编辑按钮，修改组名和颜色并保存，验证修改是否正确保存来测试。

### Tests for User Story 4 (REQUIRED per Constitution) ⚠️

- [ ] T035 [P] [US4] Create unit test for ChipRepository.UpdateChipGroupAsync in Timer/Timer.Tests/Services/ChipRepositoryTests.cs

### Implementation for User Story 4

- [ ] T036 [US4] Update ChipViewModel in Timer/Timer/ViewModels/ChipViewModel.cs to add EditChipGroupCommand (RelayCommand<ChipGroup>)
- [ ] T037 [US4] Implement EditChipGroupAsync method in Timer/Timer/ViewModels/ChipViewModel.cs to open edit dialog and save changes
- [ ] T038 [US4] Create EditChipGroupDialog.xaml in Timer/Timer/Views/EditChipGroupDialog.xaml with form fields for GroupName and Color (ComboBox with predefined color options: red, blue, green, yellow, orange, purple, etc.)
- [ ] T039 [US4] Create EditChipGroupDialog.xaml.cs in Timer/Timer/Views/EditChipGroupDialog.xaml.cs with dialog logic
- [ ] T040 [US4] Add data validation in EditChipGroupDialog (required fields: GroupName; uniqueness check for GroupName)
- [ ] T041 [US4] Add error display in EditChipGroupDialog.xaml to show validation errors
- [ ] T042 [US4] Add Edit button column in chip group DataGrid in Timer/Timer/Views/ChipView.xaml
- [ ] T043 [US4] Bind Edit button to EditChipGroupCommand in Timer/Timer/Views/ChipView.xaml
- [ ] T044 [US4] Refresh chip group list after successful edit in Timer/Timer/ViewModels/ChipViewModel.cs

**Checkpoint**: At this point, User Stories 1, 2, 3, AND 4 should all work independently - import, list display, chip details, and edit should work correctly

---

## Phase 7: User Story 5 - 芯片组和芯片删除 (Priority: P3)

**Goal**: 实现芯片组和芯片删除功能，包括确认对话框和级联删除

**Independent Test**: 可以通过选择一个芯片组或芯片，点击删除按钮，确认删除，验证是否从数据库和列表中移除来测试。

### Tests for User Story 5 (REQUIRED per Constitution) ⚠️

- [ ] T045 [P] [US5] Create unit test for ChipRepository.DeleteChipGroupAsync with cascade delete in Timer/Timer.Tests/Services/ChipRepositoryTests.cs
- [ ] T046 [P] [US5] Create unit test for ChipRepository.DeleteChipAsync in Timer/Timer.Tests/Services/ChipRepositoryTests.cs

### Implementation for User Story 5

- [ ] T047 [US5] Update ChipViewModel in Timer/Timer/ViewModels/ChipViewModel.cs to add DeleteChipGroupCommand (RelayCommand<ChipGroup>) and DeleteChipCommand (RelayCommand<Chip>)
- [ ] T048 [US5] Implement DeleteChipGroupAsync method in Timer/Timer/ViewModels/ChipViewModel.cs with confirmation dialog
- [ ] T049 [US5] Implement DeleteChipAsync method in Timer/Timer/ViewModels/ChipViewModel.cs with confirmation dialog
- [ ] T050 [US5] Add Delete button column in chip group DataGrid in Timer/Timer/Views/ChipView.xaml
- [ ] T051 [US5] Add Delete button column in chip details DataGrid in Timer/Timer/Views/ChipView.xaml
- [ ] T052 [US5] Bind Delete buttons to DeleteChipGroupCommand and DeleteChipCommand in Timer/Timer/Views/ChipView.xaml
- [ ] T053 [US5] Create confirmation dialog (MessageBox) for delete confirmation
- [ ] T054 [US5] Refresh lists after successful delete in Timer/Timer/ViewModels/ChipViewModel.cs

**Checkpoint**: At this point, all user stories should be independently functional - import, list display, chip details, edit, and delete should all work correctly

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T055 [P] Add error handling for database connection failures in Timer/Timer/Services/ChipRepository.cs
- [ ] T056 [P] Add error handling for Excel file read failures in Timer/Timer/Services/ChipImportService.cs
- [ ] T057 [P] Implement IDisposable pattern in ChipRepository in Timer/Timer/Services/ChipRepository.cs for database connection cleanup
- [ ] T058 [P] Add logging for import, edit, delete operations using ILoggingService in Timer/Timer/ViewModels/ChipViewModel.cs
- [ ] T059 [P] Update MainViewModel.CreateChipViewModel method (if exists) or update MainViewModel.InitializeMenuItems to properly inject dependencies for ChipViewModel in Timer/Timer/ViewModels/MainViewModel.cs
- [ ] T060 [P] Add input validation and sanitization for Excel import to prevent data inconsistencies
- [ ] T061 Run quickstart.md validation scenarios (if quickstart.md exists)
- [ ] T062 Code cleanup and refactoring (remove unused code, improve naming)
- [ ] T063 Update documentation comments in all ViewModels, Services, and Models
- [ ] T064 [P] Add unit tests to ensure >80% coverage in Timer/Timer.Tests/ (focus on core business logic)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Depends on US1 for data (but should be independently testable)
- **User Story 3 (P1)**: Can start after Foundational (Phase 2) - Depends on US2 for chip group selection
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Depends on US2 for chip group list
- **User Story 5 (P3)**: Can start after Foundational (Phase 2) - Depends on US2 and US3 for chip groups and chips

### Within Each User Story

- Tests (if included) MUST be written and FAIL before implementation
- Models before services
- Services before ViewModels
- ViewModels before Views
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, user stories can start in priority order
- All tests for a user story marked [P] can run in parallel
- Models within a story marked [P] can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together (if tests requested):
Task: "Create unit test for ChipImportService.ReadFromFileAsync in Timer/Timer.Tests/Services/ChipImportServiceTests.cs"
Task: "Create unit test for ChipImportService.ImportAsync in Timer/Timer.Tests/Services/ChipImportServiceTests.cs"

# Launch foundational tasks in parallel:
Task: "Create ChipGroup model in Timer/Timer/Models/ChipGroup.cs"
Task: "Create Chip model in Timer/Timer/Models/Chip.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Add User Story 4 → Test independently → Deploy/Demo
6. Add User Story 5 → Test independently → Deploy/Demo
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Excel导入)
   - Developer B: User Story 2 (芯片组列表) - after US1
   - Developer C: User Story 3 (芯片详情) - after US2
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- Excel import template format: 序号, 芯片标签号码, 芯片内部编号, 组号
- Chip group colors should be stored as ARGB hex strings (e.g., "#FF1890FF")
- Chip group deletion should cascade delete associated chips (handled by database foreign key constraint with ON DELETE CASCADE)

