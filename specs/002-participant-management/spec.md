# Feature Specification: 参赛人员管理

**Feature Branch**: `002-participant-management`  
**Created**: 2025-01-27  
**Status**: Draft  
**Input**: User description: "实现参赛人员管理功能，包括Excel导入、列表显示、编辑、删除、搜索筛选功能，数据持久化到SQLite"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Excel导入参赛人员数据 (Priority: P1)

用户需要从Excel文件（参考人员导入模板.xls格式）批量导入参赛人员数据到系统中。系统应该能够解析Excel文件，校验数据格式和完整性，并将有效数据保存到SQLite数据库。

**Why this priority**: 批量导入是参赛人员管理的核心功能，是数据录入的主要方式。没有导入功能，用户需要手动逐个添加人员，效率极低。

**Independent Test**: 可以通过准备一个符合模板格式的Excel文件，执行导入操作，验证数据是否正确解析、校验和保存到数据库来测试。导入成功后，应该能在人员列表中看到导入的数据。

**Acceptance Scenarios**:

1. **Given** 用户已打开参赛人员管理页面, **When** 用户点击"导入"按钮并选择符合模板格式的Excel文件, **Then** 系统应该解析Excel文件并显示导入预览
2. **Given** Excel文件包含有效数据, **When** 用户确认导入, **Then** 系统应该校验数据（必填字段、格式、重复性）并将有效数据保存到数据库
3. **Given** Excel文件包含无效数据（如必填字段为空、格式错误、重复记录）, **When** 用户尝试导入, **Then** 系统应该显示详细的错误信息，指出哪些行、哪些字段有问题，并拒绝导入
4. **Given** Excel文件包含部分有效和部分无效数据, **When** 用户确认导入, **Then** 系统应该导入有效数据，并显示无效数据的错误报告
5. **Given** 导入操作正在进行, **When** 系统处理数据, **Then** 应该显示进度指示，让用户了解导入状态
6. **Given** 导入完成, **When** 用户查看导入结果, **Then** 应该显示成功导入的记录数和失败记录数，以及详细的错误信息

---

### User Story 2 - 参赛人员列表显示 (Priority: P1)

用户需要能够查看所有已导入的参赛人员列表，包括人员的基本信息（姓名、性别、号码布、组别等）。列表应该支持分页显示，以便处理大量数据。

**Why this priority**: 列表显示是数据管理的基础功能，用户需要能够查看和管理已导入的数据。这是所有后续操作（编辑、删除、搜索）的基础。

**Independent Test**: 可以通过导入一些测试数据，然后打开参赛人员页面，验证列表是否正确显示所有人员信息，分页是否正常工作来测试。

**Acceptance Scenarios**:

1. **Given** 数据库中已有参赛人员数据, **When** 用户打开参赛人员管理页面, **Then** 应该显示所有人员的列表，包含姓名、性别、号码布、组别等基本信息
2. **Given** 人员数据超过一页显示数量, **When** 用户查看列表, **Then** 应该显示分页控件，允许用户翻页查看
3. **Given** 用户查看人员列表, **When** 列表加载, **Then** 应该显示加载状态，加载完成后显示数据
4. **Given** 数据库中没有人员数据, **When** 用户打开页面, **Then** 应该显示空状态提示，引导用户导入数据

---

### User Story 3 - 参赛人员信息编辑 (Priority: P2)

用户需要能够编辑单个参赛人员的信息，修改后保存到数据库。编辑时应该进行数据校验，确保数据的正确性。

**Why this priority**: 编辑功能允许用户修正导入时的错误或更新人员信息，是数据维护的重要功能。虽然可以通过重新导入来更新，但编辑功能提供了更灵活的更新方式。

**Independent Test**: 可以通过选择一个人员，打开编辑对话框，修改信息并保存，验证修改是否正确保存到数据库来测试。

**Acceptance Scenarios**:

1. **Given** 用户查看人员列表, **When** 用户点击某个人员的"编辑"按钮, **Then** 应该打开编辑对话框，显示该人员的当前信息
2. **Given** 用户在编辑对话框中修改信息, **When** 用户点击"保存", **Then** 系统应该校验数据格式，如果有效则保存到数据库并更新列表显示
3. **Given** 用户修改了必填字段为空或格式错误, **When** 用户尝试保存, **Then** 系统应该显示错误提示，阻止保存
4. **Given** 用户修改了号码布或身份证号等唯一标识, **When** 用户保存, **Then** 系统应该检查是否与其他人员重复，如果重复则拒绝保存并提示

---

### User Story 4 - 参赛人员搜索和筛选 (Priority: P2)

用户需要能够通过搜索和筛选功能快速找到特定的人员。支持按姓名、号码布、组别等条件进行搜索和筛选。

**Why this priority**: 当人员数据量大时，搜索和筛选功能是必需的，能够大大提高数据查找的效率。

**Independent Test**: 可以通过导入一些测试数据，然后使用搜索框输入关键词或选择筛选条件，验证是否能正确筛选出匹配的人员来测试。

**Acceptance Scenarios**:

1. **Given** 人员列表中有多个人员, **When** 用户在搜索框输入姓名关键词, **Then** 列表应该实时过滤，只显示匹配的人员
2. **Given** 用户需要按组别筛选, **When** 用户选择组别筛选条件, **Then** 列表应该只显示该组别的人员
3. **Given** 用户同时使用多个筛选条件, **When** 用户设置多个筛选条件, **Then** 列表应该显示同时满足所有条件的人员
4. **Given** 用户清空搜索和筛选条件, **When** 用户清除所有条件, **Then** 列表应该显示所有人员

---

### User Story 5 - 参赛人员删除 (Priority: P3)

用户需要能够删除单个或多个参赛人员。删除操作应该要求确认，避免误删。

**Why this priority**: 删除功能允许用户清理错误数据或不再需要的人员记录。虽然优先级较低，但是数据管理的基本功能。

**Independent Test**: 可以通过选择一个或多个人员，点击删除按钮，确认删除，验证人员是否从数据库和列表中移除来测试。

**Acceptance Scenarios**:

1. **Given** 用户查看人员列表, **When** 用户选择一个人员并点击"删除"按钮, **Then** 应该显示确认对话框，询问用户是否确定删除
2. **Given** 用户确认删除, **When** 用户在确认对话框中点击"确定", **Then** 系统应该从数据库删除该人员，并从列表中移除
3. **Given** 用户取消删除, **When** 用户在确认对话框中点击"取消", **Then** 应该取消删除操作，人员数据保持不变
4. **Given** 用户选择多个人员, **When** 用户执行批量删除, **Then** 应该显示确认对话框，显示将要删除的人员数量，确认后批量删除

---

### Edge Cases

- What happens when Excel文件格式不正确（不是.xls或.xlsx）？系统应该显示友好的错误提示，说明支持的文件格式
- What happens when Excel文件中的必填字段为空？系统应该拒绝导入该行，并在错误报告中明确指出
- What happens when Excel文件中的序号不连续（如1,2,4,5缺少3）？根据宪法要求，序号必须从1开始的连续数字，系统应该拒绝导入并提示
- What happens when Excel文件中的日期格式不符合要求？系统应该支持YYYY-MM-DD、YYYY-MM-DD上午、YYYY-MM-DD下午三种格式，其他格式应该拒绝并提示
- What happens when 导入的数据与数据库中已有数据重复（如相同的身份证号或号码布）？系统应该拒绝导入重复数据，并在错误报告中明确指出
- How does system handle 导入大量数据（如1000+条记录）？系统应该显示进度，使用事务确保数据一致性，如果失败应该回滚
- What happens when 用户在编辑时修改了唯一标识字段，导致与其他人员重复？系统应该拒绝保存并提示重复
- How does system handle 数据库连接失败？系统应该显示友好的错误提示，不丢失用户已输入的数据（如果可能）

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support importing participant data from Excel files (.xls, .xlsx format) matching the template structure
- **FR-002**: System MUST validate all required fields, data formats, and uniqueness constraints during import
- **FR-003**: System MUST support three date formats: YYYY-MM-DD, YYYY-MM-DD上午, YYYY-MM-DD下午
- **FR-004**: System MUST ensure sequence numbers start from 1 and are consecutive (no gaps)
- **FR-005**: System MUST reject duplicate data (based on ID number or BIB number) and provide clear error messages
- **FR-006**: System MUST use database transactions for import operations, rolling back on failure
- **FR-007**: System MUST display participant list with pagination support for large datasets
- **FR-008**: System MUST allow users to edit individual participant information with validation
- **FR-009**: System MUST support searching participants by name, BIB number, and other searchable fields
- **FR-010**: System MUST support filtering participants by group, gender, and other filterable attributes
- **FR-011**: System MUST require confirmation before deleting participants
- **FR-012**: System MUST support batch deletion of multiple participants
- **FR-013**: System MUST persist all participant data to SQLite database
- **FR-014**: System MUST display import progress and results (success count, failure count, error details)
- **FR-015**: System MUST provide clear error messages indicating which row and field has issues during import
- **FR-016**: System MUST maintain data consistency and prevent duplicate entries

### Key Entities *(include if feature involves data)*

- **Participant**: Represents a participant in the race. Key attributes include: sequence number, name, gender, date of birth, ID number, BIB number, group, chip number (optional), and other relevant information. Must have unique constraints on ID number and BIB number.

- **ImportResult**: Represents the result of an import operation. Contains: total records processed, successful records count, failed records count, and detailed error information for each failed record (row number, field name, error reason).

- **SearchFilter**: Represents search and filter criteria. Contains: search keyword, filter conditions (group, gender, etc.), and pagination parameters (page number, page size).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can successfully import participant data from Excel files matching the template format, with import completion time under 30 seconds for files with up to 500 records
- **SC-002**: System correctly validates and rejects invalid data (missing required fields, format errors, duplicates) with 100% accuracy, providing clear error messages
- **SC-003**: Users can view participant list with pagination, supporting datasets of 1000+ records without performance degradation
- **SC-004**: Users can successfully edit participant information, with data validation preventing 100% of invalid updates
- **SC-005**: Users can search and filter participants, with search results displayed within 1 second for datasets up to 1000 records
- **SC-006**: Import operations maintain data integrity with 100% transaction success rate (all or nothing)
- **SC-007**: System provides clear error feedback, with 100% of import errors clearly indicating row number and field name
- **SC-008**: Users can complete primary tasks (import, view, edit, search) with 90% success rate on first attempt without consulting documentation

