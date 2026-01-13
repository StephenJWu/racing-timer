# Tasks: WPF应用框架和导航菜单

**Input**: Design documents from `/specs/001-wpf-framework/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Per Constitution (Testing & Debugging Standards), all core logic MUST have unit tests with >80% coverage. Test tasks are included below and MUST be implemented. Navigation service MUST be abstracted through interfaces for testability.

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

**Purpose**: Project initialization and basic structure

- [ ] T001 Create project folder structure (ViewModels/, Views/, Models/, Services/, Converters/, Resources/) in Timer/Timer/
- [ ] T002 Add CommunityToolkit.Mvvm NuGet package to Timer/Timer/Timer.csproj
- [ ] T003 [P] Delete placeholder Class1.cs file from Timer/Timer/
- [ ] T004 [P] Create App.xaml application entry point in Timer/Timer/App.xaml
- [ ] T005 [P] Create App.xaml.cs application code-behind in Timer/Timer/App.xaml.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T006 Create NavigationItem model in Timer/Timer/Models/NavigationItem.cs
- [ ] T007 Create INavigationService interface in Timer/Timer/Services/INavigationService.cs
- [ ] T008 Create NavigationService implementation in Timer/Timer/Services/NavigationService.cs
- [ ] T009 [P] Create BoolToVisibilityConverter in Timer/Timer/Converters/BoolToVisibilityConverter.cs
- [ ] T010 [P] Create global styles resource file in Timer/Timer/Resources/Styles.xaml

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - 应用启动和主窗口框架 (Priority: P1) 🎯 MVP

**Goal**: 创建主窗口框架，包含左侧导航菜单区域和右侧内容区域的布局结构

**Independent Test**: 启动应用程序，验证主窗口正确显示，包含左右分栏布局（左侧约250px，右侧占据剩余空间）

### Tests for User Story 1 (REQUIRED per Constitution) ⚠️

> **NOTE: Per Constitution Testing Standards - Write these tests FIRST, ensure they FAIL before implementation. All core logic MUST achieve >80% coverage.**

- [ ] T011 [P] [US1] Create unit test for MainViewModel initialization in Timer/Timer.Tests/ViewModels/MainViewModelTests.cs
- [ ] T012 [P] [US1] Create unit test for MainWindow layout structure in Timer/Timer.Tests/Views/MainWindowTests.cs

### Implementation for User Story 1

- [ ] T013 [US1] Create MainViewModel in Timer/Timer/ViewModels/MainViewModel.cs with basic properties (MenuItems, CurrentView, SelectedMenuItem)
- [ ] T014 [US1] Create MainWindow.xaml in Timer/Timer/MainWindow.xaml with Grid layout (left navigation area 250px, right content area *)
- [ ] T015 [US1] Create MainWindow.xaml.cs code-behind in Timer/Timer/MainWindow.xaml.cs with minimal logic
- [ ] T016 [US1] Set MainWindow as startup window in Timer/Timer/App.xaml
- [ ] T017 [US1] Bind MainWindow DataContext to MainViewModel in Timer/Timer/MainWindow.xaml.cs

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently - application can start and display main window with layout

---

## Phase 4: User Story 2 - 左侧嵌套导航菜单 (Priority: P2)

**Goal**: 实现左侧导航菜单，包含所有菜单项和嵌套子菜单，支持展开/折叠，具有现代化UI风格和交互效果

**Independent Test**: 查看左侧导航菜单，验证所有菜单项显示（比赛计时、成绩管理、人员管理、设备管理），人员管理和设备管理可以展开/折叠，菜单项有hover效果和选中状态高亮

### Tests for User Story 2 (REQUIRED per Constitution) ⚠️

- [ ] T018 [P] [US2] Create unit test for NavigationItem model in Timer/Timer.Tests/Models/NavigationItemTests.cs
- [ ] T019 [P] [US2] Create unit test for menu items initialization in Timer/Timer.Tests/ViewModels/MainViewModelTests.cs (menu items setup)
- [ ] T020 [P] [US2] Create unit test for menu expand/collapse logic in Timer/Timer.Tests/ViewModels/MainViewModelTests.cs

### Implementation for User Story 2

- [ ] T021 [US2] Implement InitializeMenuItems() method in Timer/Timer/ViewModels/MainViewModel.cs to create menu structure (比赛计时, 成绩管理, 人员管理 with 参赛人员/人员分组, 设备管理 with 扫描设备/芯片设备)
- [ ] T022 [US2] Add menu items UI in Timer/Timer/MainWindow.xaml (ItemsControl or TreeView for nested menu)
- [ ] T023 [US2] Create menu item style with hover effect in Timer/Timer/Resources/Styles.xaml
- [ ] T024 [US2] Create menu item style with selected state highlighting in Timer/Timer/Resources/Styles.xaml
- [ ] T025 [US2] Implement expand/collapse command in Timer/Timer/ViewModels/MainViewModel.cs (RelayCommand for toggling IsExpanded)
- [ ] T026 [US2] Add expand/collapse animation in Timer/Timer/Resources/Styles.xaml (using WPF animations)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - navigation menu displays correctly with all items and expand/collapse functionality

---

## Phase 5: User Story 3 - 页面切换和内容区域 (Priority: P2)

**Goal**: 实现页面切换功能，点击菜单项时右侧内容区域切换到对应的页面视图

**Independent Test**: 点击不同的菜单项，验证右侧内容区域正确切换到对应页面，页面切换流畅（<500ms），无闪烁

### Tests for User Story 3 (REQUIRED per Constitution) ⚠️

- [ ] T027 [P] [US3] Create unit test for INavigationService.NavigateTo method in Timer/Timer.Tests/Services/NavigationServiceTests.cs
- [ ] T028 [P] [US3] Create unit test for navigation command in Timer/Timer.Tests/ViewModels/MainViewModelTests.cs (NavigateCommand)

### Implementation for User Story 3

- [ ] T029 [US3] Implement NavigateCommand in Timer/Timer/ViewModels/MainViewModel.cs (RelayCommand<NavigationItem>)
- [ ] T030 [US3] Implement NavigateTo method in Timer/Timer/ViewModels/MainViewModel.cs to switch CurrentView
- [ ] T031 [US3] Implement NavigationService.NavigateTo method in Timer/Timer/Services/NavigationService.cs to create/retrieve view instances
- [ ] T032 [US3] Add ContentControl binding to CurrentView in Timer/Timer/MainWindow.xaml (right content area)
- [ ] T033 [US3] Bind menu item click to NavigateCommand in Timer/Timer/MainWindow.xaml (Command binding)

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work independently - clicking menu items switches pages correctly

---

## Phase 6: User Story 4 - 占位页面视图 (Priority: P3)

**Goal**: 为所有6个功能模块创建占位页面视图，显示页面标题和"功能待实现"提示

**Independent Test**: 点击每个菜单项，验证对应的占位页面正确显示，包含清晰的标题和"功能待实现"提示

### Tests for User Story 4 (REQUIRED per Constitution) ⚠️

- [ ] T034 [P] [US4] Create unit test for RaceTimerViewModel in Timer/Timer.Tests/ViewModels/RaceTimerViewModelTests.cs
- [ ] T035 [P] [US4] Create unit test for ScoreViewModel in Timer/Timer.Tests/ViewModels/ScoreViewModelTests.cs
- [ ] T036 [P] [US4] Create unit test for ParticipantViewModel in Timer/Timer.Tests/ViewModels/ParticipantViewModelTests.cs
- [ ] T037 [P] [US4] Create unit test for GroupViewModel in Timer/Timer.Tests/ViewModels/GroupViewModelTests.cs
- [ ] T038 [P] [US4] Create unit test for DeviceViewModel in Timer/Timer.Tests/ViewModels/DeviceViewModelTests.cs
- [ ] T039 [P] [US4] Create unit test for ChipViewModel in Timer/Timer.Tests/ViewModels/ChipViewModelTests.cs

### Implementation for User Story 4

- [ ] T040 [P] [US4] Create RaceTimerViewModel in Timer/Timer/ViewModels/RaceTimerViewModel.cs with Title and Message properties
- [ ] T041 [P] [US4] Create ScoreViewModel in Timer/Timer/ViewModels/ScoreViewModel.cs with Title and Message properties
- [ ] T042 [P] [US4] Create ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs with Title and Message properties
- [ ] T043 [P] [US4] Create GroupViewModel in Timer/Timer/ViewModels/GroupViewModel.cs with Title and Message properties
- [ ] T044 [P] [US4] Create DeviceViewModel in Timer/Timer/ViewModels/DeviceViewModel.cs with Title and Message properties
- [ ] T045 [P] [US4] Create ChipViewModel in Timer/Timer/ViewModels/ChipViewModel.cs with Title and Message properties
- [ ] T046 [P] [US4] Create RaceTimerView.xaml in Timer/Timer/Views/RaceTimerView.xaml with title and "功能待实现" message
- [ ] T047 [P] [US4] Create ScoreView.xaml in Timer/Timer/Views/ScoreView.xaml with title and "功能待实现" message
- [ ] T048 [P] [US4] Create ParticipantView.xaml in Timer/Timer/Views/ParticipantView.xaml with title and "功能待实现" message
- [ ] T049 [P] [US4] Create GroupView.xaml in Timer/Timer/Views/GroupView.xaml with title and "功能待实现" message
- [ ] T050 [P] [US4] Create DeviceView.xaml in Timer/Timer/Views/DeviceView.xaml with title and "功能待实现" message
- [ ] T051 [P] [US4] Create ChipView.xaml in Timer/Timer/Views/ChipView.xaml with title and "功能待实现" message
- [ ] T052 [US4] Update NavigationService to map ViewModels to Views in Timer/Timer/Services/NavigationService.cs
- [ ] T053 [US4] Update InitializeMenuItems to associate ViewModels with menu items in Timer/Timer/ViewModels/MainViewModel.cs

**Checkpoint**: At this point, all user stories should be independently functional - all 6 placeholder pages are accessible and display correctly

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T054 [P] Add error handling for navigation failures in Timer/Timer/Services/NavigationService.cs
- [x] T055 [P] Add logging interface definition in Timer/Timer/Services/ILoggingService.cs (for future use)
- [x] T056 [P] Implement IDisposable pattern in ViewModels in Timer/Timer/ViewModels/*.cs (for resource cleanup)
- [x] T057 [P] Add responsive layout handling for window resize in Timer/Timer/MainWindow.xaml
- [ ] T058 [P] Add unit tests to ensure >80% coverage in Timer/Timer.Tests/ (Optional - can be deferred to later phases)
- [ ] T059 Run quickstart.md validation scenarios (Manual testing required)
- [x] T060 Code cleanup and refactoring (remove unused code, improve naming)
- [x] T061 Update documentation comments in all ViewModels and Services

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Depends on US1 (needs MainWindow and MainViewModel)
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Depends on US1 and US2 (needs MainWindow, MainViewModel, and menu items)
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - Depends on US3 (needs navigation working)

### Within Each User Story

- Tests (if included) MUST be written and FAIL before implementation
- Models before services
- Services before views
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T003, T004, T005)
- All Foundational tasks marked [P] can run in parallel (T009, T010)
- Once Foundational phase completes:
  - US1 tests can run in parallel (T011, T012)
  - US2 tests can run in parallel (T018, T019, T020)
  - US3 tests can run in parallel (T027, T028)
  - US4 tests can run in parallel (T034-T039)
  - US4 ViewModels can run in parallel (T040-T045)
  - US4 Views can run in parallel (T046-T051)
- All Polish tasks marked [P] can run in parallel (T054-T057, T061)

---

## Parallel Example: User Story 4

```bash
# Launch all ViewModels for User Story 4 together:
Task: "Create RaceTimerViewModel in Timer/Timer/ViewModels/RaceTimerViewModel.cs"
Task: "Create ScoreViewModel in Timer/Timer/ViewModels/ScoreViewModel.cs"
Task: "Create ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs"
Task: "Create GroupViewModel in Timer/Timer/ViewModels/GroupViewModel.cs"
Task: "Create DeviceViewModel in Timer/Timer/ViewModels/DeviceViewModel.cs"
Task: "Create ChipViewModel in Timer/Timer/ViewModels/ChipViewModel.cs"

# Launch all Views for User Story 4 together:
Task: "Create RaceTimerView.xaml in Timer/Timer/Views/RaceTimerView.xaml"
Task: "Create ScoreView.xaml in Timer/Timer/Views/ScoreView.xaml"
Task: "Create ParticipantView.xaml in Timer/Timer/Views/ParticipantView.xaml"
Task: "Create GroupView.xaml in Timer/Timer/Views/GroupView.xaml"
Task: "Create DeviceView.xaml in Timer/Timer/Views/DeviceView.xaml"
Task: "Create ChipView.xaml in Timer/Timer/Views/ChipView.xaml"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently - application can start and display main window
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP - basic window structure)
3. Add User Story 2 → Test independently → Deploy/Demo (navigation menu working)
4. Add User Story 3 → Test independently → Deploy/Demo (page switching working)
5. Add User Story 4 → Test independently → Deploy/Demo (all placeholder pages accessible)
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (main window)
   - Developer B: Prepare User Story 2 (menu structure design)
3. Once US1 is done:
   - Developer A: User Story 2 (navigation menu)
   - Developer B: User Story 3 (page switching)
4. Once US2 and US3 are done:
   - Developer A: User Story 4 (placeholder pages - ViewModels)
   - Developer B: User Story 4 (placeholder pages - Views)
5. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- All ViewModels should inherit from ObservableObject (CommunityToolkit.Mvvm)
- All commands should use RelayCommand or AsyncRelayCommand (CommunityToolkit.Mvvm)
- Follow MVVM pattern strictly - no business logic in code-behind

---

## Summary

- **Total task count**: 61 tasks
- **Task count per user story**:
  - Setup: 5 tasks
  - Foundational: 5 tasks
  - US1: 7 tasks (2 tests + 5 implementation)
  - US2: 9 tasks (3 tests + 6 implementation)
  - US3: 6 tasks (2 tests + 4 implementation)
  - US4: 20 tasks (6 tests + 14 implementation)
  - Polish: 8 tasks
- **Parallel opportunities identified**: 
  - Setup phase: 3 parallel tasks
  - Foundational phase: 2 parallel tasks
  - US4: 12 parallel tasks (6 ViewModels + 6 Views)
  - Polish phase: 5 parallel tasks
- **Independent test criteria for each story**:
  - US1: Application starts and displays main window with layout
  - US2: Navigation menu displays with all items and expand/collapse works
  - US3: Clicking menu items switches pages correctly
  - US4: All placeholder pages are accessible and display correctly
- **Suggested MVP scope**: User Story 1 only (main window framework)
- **Format validation**: ✅ ALL tasks follow the checklist format (checkbox, ID, labels, file paths)

