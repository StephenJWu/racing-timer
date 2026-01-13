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

- [ ] T001 Add Microsoft.Data.Sqlite NuGet package to Timer/Timer/Timer.csproj
- [ ] T002 Add ClosedXML NuGet package to Timer/Timer/Timer.csproj (for Excel file reading)
- [ ] T003 [P] Create Data/ folder in Timer/Timer/ for database context
- [ ] T004 [P] Create data/ folder in project root for database file storage

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Create Participant model in Timer/Timer/Models/Participant.cs with all properties (Id, SequenceNumber, Name, Gender, DateOfBirth, IdNumber, BibNumber, Group, ChipNumber, CreatedAt, UpdatedAt)
- [ ] T006 Create ImportResult model in Timer/Timer/Models/ImportResult.cs with TotalRecords, SuccessCount, FailureCount, Errors properties
- [ ] T007 Create ImportError model in Timer/Timer/Models/ImportError.cs with RowNumber, FieldName, ErrorMessage, RecordData properties
- [ ] T008 Create SearchFilter model in Timer/Timer/Models/SearchFilter.cs with SearchKeyword, Group, Gender, PageNumber, PageSize properties
- [ ] T009 Create IParticipantRepository interface in Timer/Timer/Services/IParticipantRepository.cs with all required methods (GetAllAsync, GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync, etc.)
- [ ] T010 Create DatabaseContext class in Timer/Timer/Data/DatabaseContext.cs for SQLite connection and table creation
- [ ] T011 Implement database initialization in Timer/Timer/Data/DatabaseContext.cs (CreateTables method with Participants table schema)
- [ ] T012 Create ParticipantValidator class in Timer/Timer/Services/ParticipantValidator.cs with static validation methods (Validate, ValidateRequiredFields, ValidateDateFormat, ValidateSequenceNumber, ValidateUniqueness)

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

- [ ] T017 [US1] Create IExcelImportService interface in Timer/Timer/Services/IExcelImportService.cs with ReadFromFileAsync and ImportAsync methods
- [ ] T018 [US1] Create DateConverter utility class in Timer/Timer/Converters/DateConverter.cs with ParseDate method supporting three formats (YYYY-MM-DD, YYYY-MM-DD上午, YYYY-MM-DD下午)
- [ ] T019 [US1] Implement ExcelImportService.ReadFromFileAsync in Timer/Timer/Services/ExcelImportService.cs to parse Excel file and return Participant list
- [ ] T020 [US1] Implement ExcelImportService.ImportAsync in Timer/Timer/Services/ExcelImportService.cs with transaction support, progress reporting, and error collection
- [ ] T021 [US1] Implement ParticipantRepository.AddAsync in Timer/Timer/Services/ParticipantRepository.cs with batch insert support
- [ ] T022 [US1] Implement ParticipantRepository transaction methods (BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync) in Timer/Timer/Services/ParticipantRepository.cs
- [ ] T023 [US1] Update ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs to add ImportCommand (AsyncRelayCommand), ImportProgress, ImportResult properties
- [ ] T024 [US1] Implement ImportExcelAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs with file selection, preview, validation, and import logic
- [ ] T025 [US1] Update ParticipantView.xaml in Timer/Timer/Views/ParticipantView.xaml to add import button and import result display area
- [ ] T026 [US1] Add import progress bar and result display in Timer/Timer/Views/ParticipantView.xaml
- [ ] T027 [US1] Bind import button to ImportCommand in Timer/Timer/Views/ParticipantView.xaml
- [ ] T028 [US1] Add error display control in Timer/Timer/Views/ParticipantView.xaml to show detailed import errors (row number, field name, error message)

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently - Excel import should work with validation and transaction support

---

## Phase 4: User Story 2 - 参赛人员列表显示 (Priority: P1) 🎯 MVP

**Goal**: 实现参赛人员列表显示，支持分页、加载状态和空状态提示

**Independent Test**: 可以通过导入一些测试数据，然后打开参赛人员页面，验证列表是否正确显示所有人员信息，分页是否正常工作来测试。

### Tests for User Story 2 (REQUIRED per Constitution) ⚠️

- [ ] T029 [P] [US2] Create unit test for ParticipantRepository.GetAllAsync with pagination in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs
- [ ] T030 [P] [US2] Create unit test for ParticipantRepository.GetTotalCountAsync in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs

### Implementation for User Story 2

- [ ] T031 [US2] Implement ParticipantRepository.GetAllAsync in Timer/Timer/Services/ParticipantRepository.cs with SearchFilter support and pagination (LIMIT/OFFSET)
- [ ] T032 [US2] Implement ParticipantRepository.GetTotalCountAsync in Timer/Timer/Services/ParticipantRepository.cs with SearchFilter support
- [ ] T033 [US2] Update ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs to add Participants (ObservableCollection<Participant>), IsLoading, TotalCount, CurrentPage, TotalPages properties
- [ ] T034 [US2] Implement LoadParticipantsAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs to load data with pagination
- [ ] T035 [US2] Add pagination commands (PreviousPageCommand, NextPageCommand, GoToPageCommand) in Timer/Timer/ViewModels/ParticipantViewModel.cs
- [ ] T036 [US2] Update ParticipantView.xaml in Timer/Timer/Views/ParticipantView.xaml to add DataGrid or ListView for participant list display
- [ ] T037 [US2] Add pagination controls (Previous, Next, Page number) in Timer/Timer/Views/ParticipantView.xaml
- [ ] T038 [US2] Add loading indicator in Timer/Timer/Views/ParticipantView.xaml (bind to IsLoading)
- [ ] T039 [US2] Add empty state message in Timer/Timer/Views/ParticipantView.xaml (show when Participants.Count == 0)
- [ ] T040 [US2] Bind list columns to Participant properties (Name, Gender, BibNumber, Group) in Timer/Timer/Views/ParticipantView.xaml
- [ ] T041 [US2] Call LoadParticipantsAsync in ParticipantViewModel constructor or OnNavigatedTo method

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - import and list display should work correctly

---

## Phase 5: User Story 3 - 参赛人员信息编辑 (Priority: P2)

**Goal**: 实现单个参赛人员信息的编辑功能，包括数据校验和保存

**Independent Test**: 可以通过选择一个人员，打开编辑对话框，修改信息并保存，验证修改是否正确保存到数据库来测试。

### Tests for User Story 3 (REQUIRED per Constitution) ⚠️

- [ ] T042 [P] [US3] Create unit test for ParticipantRepository.UpdateAsync in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs
- [ ] T043 [P] [US3] Create unit test for ParticipantRepository.ExistsByIdNumberAsync and ExistsByBibNumberAsync in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs

### Implementation for User Story 3

- [ ] T044 [US3] Implement ParticipantRepository.UpdateAsync in Timer/Timer/Services/ParticipantRepository.cs with validation and uniqueness check
- [ ] T045 [US3] Implement ParticipantRepository.GetByIdAsync in Timer/Timer/Services/ParticipantRepository.cs
- [ ] T046 [US3] Update ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs to add EditCommand (RelayCommand<Participant>), SelectedParticipant property
- [ ] T047 [US3] Implement EditParticipantAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs to open edit dialog and save changes
- [ ] T048 [US3] Create EditParticipantDialog.xaml in Timer/Timer/Views/EditParticipantDialog.xaml with form fields for all Participant properties
- [ ] T049 [US3] Create EditParticipantDialog.xaml.cs in Timer/Timer/Views/EditParticipantDialog.xaml.cs with dialog logic
- [ ] T050 [US3] Add data validation in EditParticipantDialog (required fields, format validation, uniqueness check)
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

- [ ] T056 [US4] Update ParticipantRepository.GetAllAsync in Timer/Timer/Services/ParticipantRepository.cs to support SearchFilter with SQL LIKE for name, BibNumber, IdNumber search
- [ ] T057 [US4] Update ParticipantRepository.GetAllAsync in Timer/Timer/Services/ParticipantRepository.cs to support Group and Gender filtering with WHERE clauses
- [ ] T058 [US4] Update ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs to add SearchFilter property and SearchCommand (RelayCommand)
- [ ] T059 [US4] Implement SearchAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs to apply search filter and reload list
- [ ] T060 [US4] Add ClearSearchCommand in Timer/Timer/ViewModels/ParticipantViewModel.cs to clear search and reload all data
- [ ] T061 [US4] Add search textbox in Timer/Timer/Views/ParticipantView.xaml with binding to SearchFilter.SearchKeyword
- [ ] T062 [US4] Add filter controls (Group dropdown, Gender dropdown) in Timer/Timer/Views/ParticipantView.xaml
- [ ] T063 [US4] Bind search button to SearchCommand in Timer/Timer/Views/ParticipantView.xaml
- [ ] T064 [US4] Bind clear search button to ClearSearchCommand in Timer/Timer/Views/ParticipantView.xaml
- [ ] T065 [US4] Implement real-time search (optional: debounce for performance) in Timer/Timer/ViewModels/ParticipantViewModel.cs

**Checkpoint**: At this point, User Stories 1, 2, 3, AND 4 should all work independently - import, list display, edit, and search/filter should work correctly

---

## Phase 7: User Story 5 - 参赛人员删除 (Priority: P3)

**Goal**: 实现单个和批量删除参赛人员功能，包括确认对话框

**Independent Test**: 可以通过选择一个或多个人员，点击删除按钮，确认删除，验证人员是否从数据库和列表中移除来测试。

### Tests for User Story 5 (REQUIRED per Constitution) ⚠️

- [ ] T066 [P] [US5] Create unit test for ParticipantRepository.DeleteAsync in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs
- [ ] T067 [P] [US5] Create unit test for ParticipantRepository.DeleteBatchAsync in Timer/Timer.Tests/Services/ParticipantRepositoryTests.cs

### Implementation for User Story 5

- [ ] T068 [US5] Implement ParticipantRepository.DeleteAsync in Timer/Timer/Services/ParticipantRepository.cs
- [ ] T069 [US5] Implement ParticipantRepository.DeleteBatchAsync in Timer/Timer/Services/ParticipantRepository.cs with transaction support
- [ ] T070 [US5] Update ParticipantViewModel in Timer/Timer/ViewModels/ParticipantViewModel.cs to add DeleteCommand (RelayCommand<Participant>) and BatchDeleteCommand (RelayCommand<IEnumerable<Participant>>)
- [ ] T071 [US5] Implement DeleteParticipantAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs with confirmation dialog
- [ ] T072 [US5] Implement BatchDeleteParticipantsAsync method in Timer/Timer/ViewModels/ParticipantViewModel.cs with confirmation dialog showing count
- [ ] T073 [US5] Add Delete button column in ParticipantView.xaml DataGrid/ListView
- [ ] T074 [US5] Add checkbox column for multi-select in ParticipantView.xaml DataGrid/ListView
- [ ] T075 [US5] Add batch delete button in Timer/Timer/Views/ParticipantView.xaml
- [ ] T076 [US5] Bind Delete button to DeleteCommand in Timer/Timer/Views/ParticipantView.xaml
- [ ] T077 [US5] Bind batch delete button to BatchDeleteCommand in Timer/Timer/Views/ParticipantView.xaml
- [ ] T078 [US5] Create confirmation dialog (MessageBox or custom dialog) for delete confirmation
- [ ] T079 [US5] Refresh list after successful delete in Timer/Timer/ViewModels/ParticipantViewModel.cs

**Checkpoint**: At this point, all user stories should be independently functional - import, list display, edit, search/filter, and delete should all work correctly

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T080 [P] Add error handling for database connection failures in Timer/Timer/Services/ParticipantRepository.cs
- [ ] T081 [P] Add error handling for Excel file read failures in Timer/Timer/Services/ExcelImportService.cs
- [ ] T082 [P] Implement IDisposable pattern in ParticipantRepository in Timer/Timer/Services/ParticipantRepository.cs for database connection cleanup
- [ ] T083 [P] Add logging for import, edit, delete operations using ILoggingService in Timer/Timer/ViewModels/ParticipantViewModel.cs
- [ ] T084 [P] Add database health check method in Timer/Timer/Services/ParticipantRepository.cs
- [ ] T085 [P] Optimize SQL queries with proper indexes (already defined in schema, verify in DatabaseContext)
- [ ] T086 [P] Add input validation and sanitization for search keywords to prevent SQL injection
- [ ] T087 Run quickstart.md validation scenarios
- [ ] T088 Code cleanup and refactoring (remove unused code, improve naming)
- [ ] T089 Update documentation comments in all ViewModels, Services, and Models
- [ ] T090 [P] Add unit tests to ensure >80% coverage in Timer/Timer.Tests/ (focus on core business logic)

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
- Excel template structure needs to be confirmed from actual template file
- Date format support: YYYY-MM-DD, YYYY-MM-DD上午, YYYY-MM-DD下午
- Sequence number must start from 1 and be consecutive
- All database operations must use transactions for data consistency
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence

