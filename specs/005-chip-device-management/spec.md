# Feature Specification: 芯片设备管理

**Feature Branch**: `005-chip-device-management`  
**Created**: 2025-01-14  
**Status**: Draft  
**Input**: User description: "芯片设备模块：支持从Excel导入芯片信息，管理芯片组（包括颜色设置），查看芯片详情列表"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Excel导入芯片信息 (Priority: P1)

用户需要从Excel文件批量导入芯片信息到系统中。系统应该能够解析Excel文件，校验数据格式和完整性，并将芯片信息按芯片组分类保存。

**Why this priority**: 批量导入是芯片设备管理的核心功能，是数据录入的主要方式。没有导入功能，用户需要手动逐个添加芯片，效率极低。

**Independent Test**: 可以通过准备一个符合模板格式的Excel文件（包含序号、芯片标签号码、芯片内部编号、组号列），执行导入操作，验证数据是否正确解析、校验和保存到数据库来测试。导入成功后，应该能在芯片组列表中看到导入的芯片组和芯片详情。

**Acceptance Scenarios**:

1. **Given** 用户已打开芯片设备管理页面, **When** 用户点击"导入芯片信息"按钮并选择符合模板格式的Excel文件, **Then** 系统应该解析Excel文件并验证表头格式（序号、芯片标签号码、芯片内部编号、组号）
2. **Given** Excel文件包含有效数据, **When** 用户确认导入, **Then** 系统应该校验数据（非空、芯片标签号码不重复）并将芯片信息按组名分组保存
3. **Given** Excel文件包含无效数据（如必填字段为空、芯片标签号码重复）, **When** 用户尝试导入, **Then** 系统应该显示详细的错误信息，指出哪些行、哪些字段有问题，并拒绝导入
4. **Given** Excel文件包含多个芯片组的数据, **When** 用户导入, **Then** 系统应该按组名自动分组，如果芯片组不存在则自动创建芯片组
5. **Given** 导入操作正在进行, **When** 系统处理数据, **Then** 应该显示进度指示，让用户了解导入状态
6. **Given** 导入完成, **When** 用户查看导入结果, **Then** 应该显示成功导入的记录数和失败记录数，以及详细的错误信息

---

### User Story 2 - 芯片组列表显示和管理 (Priority: P1)

用户需要能够查看所有芯片组列表，包括芯片组名称、颜色、芯片数量等信息。列表应该以表格形式展示，支持选择芯片组查看详情。

**Why this priority**: 芯片组列表是芯片设备管理的基础功能，用户需要能够查看和管理已导入的芯片组。这是所有后续操作（查看详情、编辑、删除）的基础。

**Independent Test**: 可以通过导入一些测试数据，然后打开芯片设备页面，验证列表是否正确显示所有芯片组信息，选择芯片组后是否正确显示芯片详情来测试。

**Acceptance Scenarios**:

1. **Given** 数据库中已有芯片组数据, **When** 用户打开芯片设备管理页面, **Then** 应该在上半部分显示芯片组列表表格，包含列：选择、组名、组颜色、数量、操作
2. **Given** 芯片组列表已显示, **When** 用户选择一个芯片组（通过表格选择）, **Then** 下半部分应该显示该芯片组的芯片详情表格
3. **Given** 用户查看芯片组列表, **When** 列表加载, **Then** 应该显示加载状态，加载完成后显示数据
4. **Given** 数据库中没有芯片组数据, **When** 用户打开页面, **Then** 应该显示空状态提示，引导用户导入数据
5. **Given** 用户查看芯片组列表, **When** 查看组颜色列, **Then** 应该以颜色块的形式显示芯片组的颜色

---

### User Story 3 - 芯片详情查看 (Priority: P1)

用户需要能够查看选中芯片组的所有芯片详情，包括芯片标签号码和芯片内部号码。

**Why this priority**: 查看芯片详情是芯片管理的核心功能，用户需要能够查看每个芯片组中包含哪些芯片，以便进行管理和分配。

**Independent Test**: 可以通过导入一些测试数据，选择一个芯片组，验证是否能够正确显示该芯片组的所有芯片详情来测试。

**Acceptance Scenarios**:

1. **Given** 用户已选择一个芯片组, **When** 查看芯片详情区域, **Then** 应该显示该芯片组的所有芯片列表，包含列：芯片标签号码、芯片内部号码、操作
2. **Given** 用户未选择任何芯片组, **When** 查看芯片详情区域, **Then** 应该显示"请选择上方芯片组查看详情"的提示
3. **Given** 用户选择不同的芯片组, **When** 切换选择, **Then** 芯片详情区域应该更新显示新选中芯片组的芯片列表
4. **Given** 选中的芯片组包含大量芯片, **When** 查看芯片详情, **Then** 应该能够正常显示所有芯片，支持滚动查看

---

### User Story 4 - 芯片组编辑 (Priority: P2)

用户需要能够编辑芯片组信息，包括芯片组名称和颜色设置。

**Why this priority**: 编辑功能允许用户修正导入时的错误或更新芯片组信息，是数据维护的重要功能。

**Independent Test**: 可以通过选择一个芯片组，点击编辑按钮，修改组名和颜色并保存，验证修改是否正确保存来测试。

**Acceptance Scenarios**:

1. **Given** 用户查看芯片组列表, **When** 用户点击某个芯片组的"编辑"按钮, **Then** 应该打开编辑对话框，显示该芯片组的当前信息（组名、颜色）
2. **Given** 用户在编辑对话框中修改信息, **When** 用户点击"保存", **Then** 系统应该校验数据格式，如果有效则保存并更新列表显示
3. **Given** 用户修改芯片组颜色, **When** 用户选择颜色并保存, **Then** 芯片组列表中的颜色块应该更新为新的颜色
4. **Given** 用户修改了组名为已存在的名称, **When** 用户尝试保存, **Then** 系统应该显示错误提示，阻止保存

---

### User Story 5 - 芯片组和芯片删除 (Priority: P3)

用户需要能够删除芯片组或单个芯片。删除操作应该要求确认，避免误删。

**Why this priority**: 删除功能允许用户清理错误数据或不再需要的芯片记录。虽然优先级较低，但是数据管理的基本功能。

**Independent Test**: 可以通过选择一个芯片组或芯片，点击删除按钮，确认删除，验证是否从数据库和列表中移除来测试。

**Acceptance Scenarios**:

1. **Given** 用户查看芯片组列表, **When** 用户点击某个芯片组的"删除"按钮, **Then** 应该显示确认对话框，询问用户是否确定删除
2. **Given** 用户确认删除芯片组, **When** 用户在确认对话框中点击"确定", **Then** 系统应该从数据库删除该芯片组及其关联的芯片，并从列表中移除
3. **Given** 用户取消删除, **When** 用户在确认对话框中点击"取消", **Then** 应该取消删除操作，数据保持不变
4. **Given** 用户查看芯片详情, **When** 用户点击某个芯片的"删除"按钮, **Then** 应该显示确认对话框，确认后删除该芯片

---

### Edge Cases

- What happens when Excel文件格式不正确（不是.xls或.xlsx）？系统应该显示友好的错误提示，说明支持的文件格式
- What happens when Excel文件中的必填字段为空？系统应该拒绝导入该行，并在错误报告中明确指出
- What happens when Excel文件中的芯片标签号码重复？系统应该拒绝导入重复数据，并在错误报告中明确指出
- What happens when 导入的芯片组名称与数据库中已有芯片组名称重复？系统应该将芯片添加到已有芯片组，而不是创建新的芯片组
- How does system handle 导入大量数据（如1000+条记录）？系统应该显示进度，使用事务确保数据一致性，如果失败应该回滚
- What happens when 用户在编辑芯片组时修改了组名，导致与其他芯片组名称重复？系统应该拒绝保存并提示重复
- How does system handle 删除包含芯片的芯片组？系统应该级联删除关联的芯片，或提示用户先删除所有芯片
- What happens when 用户选择颜色后未保存就关闭编辑对话框？系统应该丢弃未保存的更改，保持原有数据

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow users to import chip information from Excel files
- **FR-002**: System MUST validate Excel file format (columns: 序号, 芯片标签号码, 芯片内部编号, 组号)
- **FR-003**: System MUST validate imported data (non-empty fields, unique chip label numbers)
- **FR-004**: System MUST group chips by group name during import
- **FR-005**: System MUST automatically create chip groups if they don't exist during import
- **FR-006**: System MUST display chip group list in table format with columns: selection, group name, group color, chip count, actions
- **FR-007**: System MUST display chip details for selected chip group
- **FR-008**: System MUST allow users to select a chip group to view its chip details
- **FR-009**: System MUST display chip group color as a color block in the list
- **FR-010**: System MUST allow users to edit chip group information (name and color)
- **FR-011**: System MUST provide predefined color options for chip group color selection
- **FR-012**: System MUST validate chip group name uniqueness when editing
- **FR-013**: System MUST allow users to delete chip groups with confirmation
- **FR-014**: System MUST allow users to delete individual chips with confirmation
- **FR-015**: System MUST cascade delete chips when deleting a chip group
- **FR-016**: System MUST display import progress and results (success count, failure count, error details)
- **FR-017**: System MUST display empty state when no chip groups exist
- **FR-018**: System MUST display "Please select a chip group to view details" when no chip group is selected

### Key Entities *(include if feature involves data)*

- **ChipGroup**: Represents a group of chips with a name and color. Key attributes: Id, GroupName, Color, ChipCount, CreatedAt, UpdatedAt
- **Chip**: Represents an individual chip with label number and internal number. Key attributes: Id, ChipGroupId, LabelNumber, InternalNumber, CreatedAt, UpdatedAt

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can successfully import chip data from Excel files with validation and error reporting
- **SC-002**: System displays chip group list within 300ms after page load
- **SC-003**: System displays chip details within 200ms after chip group selection
- **SC-004**: Users can complete chip group editing (name and color change) in under 30 seconds
- **SC-005**: System processes Excel import with 100+ records within 5 seconds
- **SC-006**: System maintains data integrity (no duplicate chip label numbers, valid group relationships)
- **SC-007**: All import errors are clearly reported with row numbers and field names
- **SC-008**: Chip group color changes are immediately reflected in the list display
