# Shift Management API

## Endpoints

### 1. Get Shifts
```http
GET /api/v1/shifts
```

**Purpose**: Get paginated list of study shifts with search and filter.
**Roles**: `OWNER`

**Query Parameters**:
- `keyword` (optional): Search by shift code or name
- `status` (optional): Filter by status (Active, Inactive)
- `pageNumber` (optional): Default 1
- `pageSize` (optional): Default 20

**Response**:
```json
{
  "success": true,
  "message": "Success",
  "data": {
    "items": [
      {
        "studyShiftId": 1,
        "shiftCode": "S1",
        "shiftName": "Ca 1",
        "startTime": "07:30:00",
        "endTime": "09:00:00",
        "status": "Active",
        "note": null
      }
    ],
    "totalItems": 1,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```

### 2. Create Shift
```http
POST /api/v1/shifts
```

**Purpose**: Create a new study shift.
**Roles**: `OWNER`

**Request**:
```json
{
  "shiftCode": "S1",
  "shiftName": "Ca 1",
  "startTime": "07:30:00",
  "endTime": "09:00:00",
  "status": "Active",
  "note": "Sáng"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Thêm ca học thành công.",
  "data": {
    "studyShiftId": 1,
    "status": "Active"
  }
}
```

### 3. Update Shift
```http
PUT /api/v1/shifts/{id}
```

**Purpose**: Update an existing shift.
**Roles**: `OWNER`

**Request**:
```json
{
  "shiftCode": "S1",
  "shiftName": "Ca 1",
  "startTime": "07:30:00",
  "endTime": "09:30:00",
  "status": "Active",
  "note": "Sáng VIP"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Cập nhật ca học thành công.",
  "data": {
    "studyShiftId": 1,
    "status": "Active"
  }
}
```

### 4. Update Shift Status
```http
PATCH /api/v1/shifts/{id}/status
```

**Purpose**: Change shift status.
**Roles**: `OWNER`

**Request**:
```json
{
  "status": "Inactive"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Đổi trạng thái ca học thành công.",
  "data": {
    "studyShiftId": 1,
    "status": "Inactive"
  }
}
```

### 5. Delete Shift
```http
DELETE /api/v1/shifts/{id}
```

**Purpose**: Soft delete an inactive shift.
**Roles**: `OWNER`

**Response**:
```json
{
  "success": true,
  "message": "Xóa ca học thành công.",
  "data": true
}
```

## Validation Rules
- `shiftCode`: Required, max 50 chars, must be unique within center.
- `shiftName`: Required, max 100 chars.
- `startTime`: Required, must be before `endTime`.
- `endTime`: Required, must be after `startTime`.
- `status`: Must be "Active" or "Inactive".

## Error Cases
- Shift code already exists.
- Cannot pause/delete shift if there are classes currently using it.
- Cannot update times if there are classes currently using it.
- Shift not found or unauthorized.
