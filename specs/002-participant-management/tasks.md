# Tasks: 参赛人员管理

**Input**: Design documents from `/specs/002-participant-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

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

**Purpose**: Project initialization and dependencies

- [x] T001 Add Microsoft.Data.Sqlite NuGet package to Timer/Timer/Timer.csproj
- [x] T002 Add ClosedXML NuGet package to Timer/Timer/Timer.csproj (for Excel file reading)
- [x] T003 [P] Create Data/ folder in Timer/Timer/ for database context
- [x] T004 [P] Create data/ folder in project root for database file storage

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Create Participant model in Timer/Timer/Models/Participant.cs with all properties (Id, SequenceNumber, Date, School, Grade, Class, Name, Gender, ExamNumber, GroupName, BibNumber, ChipNumber, CreatedAt, UpdatedAt)
- [x] T006 Create ImportResult model in Timer/Timer/Models/ImportResult.cs with TotalRecords, SuccessCount, FailureCount, Errors properties
- [x] T007 Create ImportError model in Timer/Timer/Models/ImportError.cs with RowNumber, FieldName, ErrorMessage, RecordData properties
- [x] T008 Create SearchFilter model in Timer/Timer/Models/SearchFilter.cs with SearchKeyword, Group, Gender, PageNumber, PageSize properties (已扩展支持StartDate, EndDate, School, Grade, Class)
- [x] T009 Create IParticipantRepository interface in Timer/Timer/Services/IParticipantRepository.cs with all required methods (GetAllAsync, GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync, etc.)
- [x] T010 Create DatabaseContext class in Timer/Timer/Data/DatabaseContext.cs for SQLite connection and table creation
- [x] T011 Implement database initialization in Timer/Timer/Data/DatabaseContext.cs (CreateTables method with Participants table schema)
- [x] T012 Create ParticipantValidator class in Timer/Timer/Services/ParticipantValidator.cs with static validation methods (Validate, ValidateRequiredFields, ValidateDateFormat supporting YYYY-M-D and YYYY-MM-DD variants, ValidateSequenceNumber, ValidateUniqueness for ExamNumber)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Excel导入参赛人员数据 (Priority: P1) 🎯 MVP

**Goal**: 实现从Excel文件批量导入参赛人员数据，包括文件解析、数据校验、事务性保存和进度报告

**Independent Test**: 可以通过准备一个符合模板格式的Excel文件，执行导入操作，验证数据是否正确解析、校验和保存到数据库来测试。导入成功后，应该能在人员列表中看到导入的数据。

### Tests for User Story 1 (REQUIRED per Constitution) ⚠️

> **NOTE: Per Constitution Testing Standards - Write these tests FIRST, ensure they FAIL before implementation. All core logic MUST achieve >80% coverage.**

- [ ] T013 [P] [US1] Create unit test for ParticipantValidator.ValidateRequiredFields in Timer/Timer.Tests/Services/ParticipantValidatorTests.cs
- [ ] T014 [P] [US1] Create unit test for ParticipantValidator.ValidateDateFormat in Timer/Timer.Tests/Services/ParticipantValidatorTests.cs
- [ ] T015 [P] [US1] Create unit test for ParticipantValidator.ValidateSequenceNumber in Timer/Timer.Tests/Services/ParticipantValidatorTests.cs
- [ ] T016 [P] [US1] Create unit test for ExcelImportService.ReadFromFileAsync in Timer/Timer.Tests/Services/ExcelImportServiceTests.cs

### Implementation for User Story 1

- [x] T017 [US1] Create IExcelImportService interface in Timer/Timer/Services/IExcelImportService.cs with ReadFromFileAsync and ImportAsync methods
- [x] T018 [US1] Create DateConverter utility class in Timer/Timer/Converters/DateConverter.cs with ParseDate method supporting multiple formats (YYYY-M-D, YYYY-MM-DD, YYYY-M-D上午, YYYY-MM-DD上午, YYYY-M-D下午, YYYY-MM-DD下午)
- [x] T019 [US1] Implement ExcelImportService.ReadFromFileAsync in Timer/Timer/Services/ExcelImportService.cs to parse Excel file with columns (序号, 日期, 学校, 年级, 班级, 姓名, 性别, 准考证号, 组别名称) and return Participant list
- [x] T020 [US1] Implement ExcelImportService.ImportAsync in Timer/Timer/Services/ExcelImportService.cs with transaction support, progress reporting, and error collection
- [x] T021 [US1] Implement ParticipantRepository.AddAsync in Timer/Timer/Services/ParticipantRepository.cs with batch insert support
- [x] T022 [US1] Implement ParticipantRepository transaction methods (BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync) in Timer/Timer/Services/ParticipantRepository.cs
- [x] T023 [US1] Update ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs to add ImportCommand (AsyncRelayCommand), ImportProgress, ImportResult properties
- [x] T024 [US1] Implement ImportExcelAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs with file selection, preview, validation, and import logic
- [x] T025 [US1] Update ParticipantView.xaml in Timer/Timer/Views/ParticipantView.xaml to add import button and import result display area
- [x] T026 [US1] Add import progress bar and result display in Timer/Timer/Views/ParticipantView.xaml
- [x] T027 [US1] Bind import button to ImportCommand in Timer/Timer/Views/ParticipantView.xaml
- [x] T028 [US1] Add error display control in Timer/Timer/Views/ParticipantView.xaml to show detailed import errors (row number, field name, error message)

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently - Excel import should work with validation and transaction support

---

## Phase 4: User Story 2 - 参赛人员列表显示 (Priority: P1) 🎯 MVP

**Goal**: 实现参赛人员列表显示，支持分页、加载状态和空状态提示

**Independent Test**: 可以通过导入一些测试数据，然后打开参赛人员页面，验证列表是否正确显示所有人员信息，分页是否正常工作来测试。

### Tests for User Story 2 (REQUIRED per Constitution) ⚠️

- [ ] T029 [P] [US2] Create unit test for ParticipantRepository.GetAllAsync with pagination in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs
- [ ] T030 [P] [US2] Create unit test for ParticipantRepository.GetTotalCountAsync in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs

### Implementation for User Story 2

- [x] T031 [US2] Implement ParticipantRepository.GetAllAsync in Timer/Timer/Services/ParticipantRepository.cs with SearchFilter support and pagination (LIMIT/OFFSET)
- [x] T032 [US2] Implement ParticipantRepository.GetTotalCountAsync in Timer/Timer/Services/ParticipantRepository.cs with SearchFilter support
- [x] T033 [US2] Update ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs to add Participants (ObservableCollection<Participant>), IsLoading, TotalCount, CurrentPage, TotalPages properties
- [x] T034 [US2] Implement LoadParticipantsAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs to load data with pagination
- [x] T035 [US2] Add pagination commands (PreviousPageCommand, NextPageCommand, GoToPageCommand) in Timer/Timer/ViewModels/ParticipantViewModel.cs
- [x] T036 [US2] Update ParticipantView.xaml in Timer/Timer/Views/ParticipantView.xaml to add DataGrid or ListView for participant list display
- [x] T037 [US2] Add pagination controls (Previous, Next, Page number) in Timer/Timer/Views/ParticipantView.xaml
- [x] T038 [US2] Add loading indicator in Timer/Timer/Views/ParticipantView.xaml (bind to IsLoading)
- [x] T039 [US2] Add empty state message in Timer/Timer/Views/ParticipantView.xaml (show when Participants.Count == 0)
- [x] T040 [US2] Bind list columns to Participant properties (SequenceNumber, Name, Gender, ExamNumber, GroupName, School, Grade, Class) in Timer/Timer/Views/ParticipantView.xaml
- [x] T041 [US2] Call LoadParticipantsAsync in ParticipantViewModel constructor or OnNavigatedTo method

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - import and list display should work correctly

---

## Phase 5: User Story 3 - 参赛人员信息编辑 (Priority: P2)

**Goal**: 实现单个参赛人员信息的编辑功能，包括数据校验和保存

**Independent Test**: 可以通过选择一个人员，打开编辑对话框，修改信息并保存，验证修改是否正确保存到数据库来测试。

### Tests for User Story 3 (REQUIRED per Constitution) ⚠️

- [ ] T042 [P] [US3] Create unit test for ParticipantRepository.UpdateAsync in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs
- [ ] T043 [P] [US3] Create unit test for ParticipantRepository.ExistsByExamNumberAsync and ExistsByBibNumberAsync in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs

### Implementation for User Story 3

- [ ] T044 [US3] Implement ParticipantRepository.UpdateAsync in Timer/Timer/Services/ParticipantRepository.cs with validation and uniqueness check
- [ ] T045 [US3] Implement ParticipantRepository.GetByIdAsync in Timer/Timer/Services/ParticipantRepository.cs
- [ ] T046 [US3] Update ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs to add EditCommand (RelayCommand<Participant>), SelectedParticipant property
- [ ] T047 [US3] Implement EditParticipantAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs to open edit dialog and save changes
- [ ] T048 [US3] Create EditParticipantDialog.xaml in Timer/Timer/Views/EditParticipantDialog.xaml with form fields for all Participant properties
- [ ] T049 [US3] Create EditParticipantDialog.xaml.cs in Timer/Timer/Views/EditParticipantDialog.xaml.cs with dialog logic
- [ ] T050 [US3] Add data validation in EditParticipantDialog (required fields: Name, Gender, Date; format validation; uniqueness check for ExamNumber)
- [ ] T051 [US3] Add error display in EditParticipantDialog.xaml to show validation errors
- [ ] T052 [US3] Add Edit button column in ParticipantView.xaml DataGrid/ListView
- [ ] T053 [US3] Bind Edit button to EditCommand in Timer/Timer/Views/ParticipantView.xaml
- [ ] T054 [US3] Refresh list after successful edit in Timer/Timer/ViewModels/ParticipantViewModel.cs

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work independently - import, list display, and edit should work correctly

---

## Phase 6: User Story 4 - 参赛人员搜索和筛选 (Priority: P2)

**Goal**: 实现搜索和筛选功能，支持按姓名、号码布、组别等条件过滤人员列表

**Independent Test**: 可以通过导入一些测试数据，然后使用搜索框输入关键词或选择筛选条件，验证是否能正确筛选出匹配的人员来测试。

### Tests for User Story 4 (REQUIRED per Constitution) ⚠️

- [ ] T055 [P] [US4] Create unit test for ParticipantRepository.GetAllAsync with SearchFilter (search keyword, group filter, gender filter) in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs

### Implementation for User Story 4

- [x] T056 [US4] Update ParticipantRepository.GetAllAsync in Timer/Timer/Services/ParticipantRepository.cs to support SearchFilter with SQL LIKE for name, ExamNumber, BibNumber search
- [x] T057 [US4] Update ParticipantRepository.GetAllAsync in Timer/Timer/Services/ParticipantRepository.cs to support GroupName, Gender, School filtering with WHERE clauses (已扩展支持StartDate, EndDate, School, Grade, Class级联筛选)
- [x] T058 [US4] Update ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs to add SearchFilter property and SearchCommand (RelayCommand)
- [x] T059 [US4] Implement SearchAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs to apply search filter and reload list
- [x] T060 [US4] Add ClearSearchCommand in Timer/Timer/ViewModels/ParticipantViewModel.cs to clear search and reload all data
- [x] T061 [US4] Add search textbox in Timer/Timer/Views/ParticipantView.xaml with binding to SearchFilter.SearchKeyword (已改为日期范围筛选和级联下拉)
- [x] T062 [US4] Add filter controls (GroupName dropdown, Gender dropdown, School dropdown) in Timer/Timer/Views/ParticipantView.xaml (已实现日期范围、学校/年级/班级/组别级联下拉)
- [x] T063 [US4] Bind search button to SearchCommand in Timer/Timer/Views/ParticipantView.xaml
- [x] T064 [US4] Bind clear search button to ClearSearchCommand in Timer/Timer/ViewModels/ParticipantViewModel.cs (已实现)
- [x] T065 [US4] Implement real-time search (optional: debounce for performance) in Timer/Timer/ViewModels/ParticipantViewModel.cs (已实现级联下拉联动)

**Checkpoint**: At this point, User Stories 1, 2, 3, AND 4 should all work independently - import, list display, edit, and search/filter should work correctly

---

## Phase 7: User Story 5 - 参赛人员删除 (Priority: P3)

**Goal**: 实现单个和批量删除参赛人员功能，包括确认对话框

**Independent Test**: 可以通过选择一个或多个人员，点击删除按钮，确认删除，验证人员是否从数据库和列表中移除来测试。

### Tests for User Story 5 (REQUIRED per Constitution) ⚠️

- [ ] T066 [P] [US5] Create unit test for ParticipantRepository.DeleteAsync in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs
- [ ] T067 [P] [US5] Create unit test for ParticipantRepository.DeleteBatchAsync in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs

### Implementation for User Story 5

- [x] T068 [US5] Implement ParticipantRepository.DeleteAsync in Timer/Timer/Services/ParticipantRepository.cs
- [x] T069 [US5] Implement ParticipantRepository.DeleteBatchAsync in Timer/Timer/Services/ParticipantRepository.cs
- [x] T070 [US5] Update ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs to add DeleteCommand and BatchDeleteCommand
- [x] T071 [US5] Implement DeleteParticipantAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs with confirmation dialog
- [x] T072 [US5] Implement BatchDeleteParticipantsAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs with confirmation dialog showing count
- [x] T073 [US5] Add Delete button column in ParticipantView.xaml DataGrid
- [x] T074 [US5] Add checkbox column for multi-select in ParticipantView.xaml DataGrid (基于Participant.IsSelected)
- [x] T075 [US5] Add batch delete button in Timer/Timer/Views/ParticipantView.xaml
- [x] T076 [US5] Bind Delete button to DeleteCommand in Timer/Timer/Views/ParticipantView.xaml
- [x] T077 [US5] Bind batch delete button to BatchDeleteCommand in Timer/Timer/Views/ParticipantView.xaml
- [x] T078 [US5] Create confirmation dialog (MessageBox) for delete confirmation
- [x] T079 [US5] Refresh list after successful delete in Timer/Timer/ViewModels/ParticipantViewModel.cs

**Checkpoint**: At this point, all user stories should be independently functional - import, list display, edit, search/filter, and delete should all work correctly

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T080 [P] Add error handling for database connection failures in Timer/Timer/Services/ParticipantRepository.cs
- [x] T081 [P] Add error handling for Excel file read failures in Timer/Timer/Services/ExcelImportService.cs
- [x] T082 [P] Implement IDisposable pattern in ParticipantRepository in Timer/Timer/Services/ParticipantRepository.cs for database connection cleanup
- [x] T083 [P] Add logging for import, edit, delete operations using ILoggingService in Timer/Timer/ViewModels/ParticipantViewModel.cs
- [ ] T084 [P] Add database health check method in Timer/Timer/Services/ParticipantRepository.cs
- [x] T085 [P] Optimize SQL queries with proper indexes (already defined in schema, verify in DatabaseContext)
- [x] T086 [P] Add input validation and sanitization for search keywords to prevent SQL injection (使用参数化查询)
- [ ] T087 Run quickstart.md validation scenarios
- [x] T088 Code cleanup and refactoring (remove unused code, improve naming)
- [x] T089 Update documentation comments in all ViewModels, Services, and Models
- [ ] T090 [P] Add unit tests to ensure >80% coverage in Timer/Timer.Tests/ (focus on core business logic)

### UI/UX Improvements (Completed)

- [x] T091 [P] Create DateDisplayConverter in Timer/Timer/Converters/DateDisplayConverter.cs for yyyy-M-d date format display
- [x] T092 [P] Update App.xaml.cs to set CultureInfo for consistent DatePicker date format (yyyy-MM-dd)
- [x] T093 [P] Fix DataGrid double horizontal lines issue by removing GridLinesVisibility="Horizontal" from ParticipantView.xaml
- [x] T094 [P] Fix ComboBox dropdown display issue by removing incomplete ControlTemplate from Styles.xaml
- [x] T095 [P] Optimize DataGridRow style to remove bottom border, keeping only DataGridCell's fine separator lines
- [x] T096 [P] Implement cascading dropdowns (School -> Grade -> Class -> Group) in ParticipantViewModel with GetDistinct*Async methods
- [x] T097 [P] Update ParticipantRepository to support date range filtering using substr(Date,1,10) for accurate date comparison
- [x] T098 [P] Refresh cascading dropdown data after successful Excel import in ParticipantViewModel

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Depends on US1 for data (needs imported data to display), but can be tested independently with test data
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Depends on US2 (needs list display to select participant for editing)
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Depends on US2 (needs list display to apply search/filter)
- **User Story 5 (P3)**: Can start after Foundational (Phase 2) - Depends on US2 (needs list display to select participant for deletion)

### Within Each User Story

- Tests (if included) MUST be written and FAIL before implementation
- Models before services
- Services before ViewModel
- ViewModel before View
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, User Stories 1 and 2 can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members (with coordination)

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Create unit test for ParticipantValidator.ValidateRequiredFields"
Task: "Create unit test for ParticipantValidator.ValidateDateFormat"
Task: "Create unit test for ParticipantValidator.ValidateSequenceNumber"
Task: "Create unit test for ExcelImportService.ReadFromFileAsync"

# Models and utilities can be created in parallel:
Task: "Create ImportResult model"
Task: "Create ImportError model"
Task: "Create DateConverter utility class"
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Excel Import)
4. Complete Phase 4: User Story 2 (List Display)
5. **STOP and VALIDATE**: Test User Stories 1 & 2 independently
6. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (Import MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo (List Display MVP!)
4. Add User Story 3 → Test independently → Deploy/Demo
5. Add User Story 4 → Test independently → Deploy/Demo
6. Add User Story 5 → Test independently → Deploy/Demo
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Excel Import)
   - Developer B: User Story 2 (List Display) - can use test data
3. After US1 and US2 complete:
   - Developer A: User Story 3 (Edit)
   - Developer B: User Story 4 (Search/Filter)
4. Developer C: User Story 5 (Delete)
5. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Database file location: `data/timer.db` (create data/ folder if not exists)
- Excel template columns: 序号, 日期, 学校, 年级, 班级, 姓名, 性别, 准考证号, 组别名称
- Date format support: YYYY-M-D, YYYY-MM-DD, YYYY-M-D上午, YYYY-MM-DD上午, YYYY-M-D下午, YYYY-MM-DD下午 (支持单数字和双数字格式)
- Sequence number must start from 1 and be consecutive
- All database operations must use transactions for data consistency
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence

