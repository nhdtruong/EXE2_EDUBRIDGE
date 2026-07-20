# Room Management API

## Endpoints

### 1. Get Rooms
```http
GET /api/v1/rooms
```

**Purpose**: Get paginated list of rooms with search and filter.
**Roles**: `OWNER`

**Query Parameters**:
- `keyword` (optional): Search by room code or name
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
        "roomId": 1,
        "roomCode": "R01",
        "roomName": "Room 01",
        "capacity": 30,
        "location": "1st Floor",
        "status": "Active"
      }
    ],
    "totalItems": 1,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```

### 2. Create Room
```http
POST /api/v1/rooms
```

**Purpose**: Create a new room.
**Roles**: `OWNER`

**Request**:
```json
{
  "roomCode": "R01",
  "roomName": "Room 01",
  "capacity": 30,
  "location": "1st Floor",
  "status": "Active"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Thêm phòng học thành công.",
  "data": {
    "roomId": 1,
    "status": "Active"
  }
}
```

### 3. Update Room
```http
PUT /api/v1/rooms/{id}
```

**Purpose**: Update an existing room.
**Roles**: `OWNER`

**Request**:
```json
{
  "roomCode": "R01",
  "roomName": "Room 01 VIP",
  "capacity": 35,
  "location": "1st Floor",
  "status": "Active"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Cập nhật phòng học thành công.",
  "data": {
    "roomId": 1,
    "status": "Active"
  }
}
```

### 4. Update Room Status
```http
PATCH /api/v1/rooms/{id}/status
```

**Purpose**: Change room status.
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
  "message": "Đổi trạng thái phòng học thành công.",
  "data": {
    "roomId": 1,
    "status": "Inactive"
  }
}
```

### 5. Delete Room
```http
DELETE /api/v1/rooms/{id}
```

**Purpose**: Soft delete an inactive room.
**Roles**: `OWNER`

**Response**:
```json
{
  "success": true,
  "message": "Xóa phòng học thành công.",
  "data": true
}
```

## Validation Rules
- `roomCode`: Required, max 30 chars, must be unique among non-deleted rooms within the center.
- `roomName`: Required, max 100 chars.
- `capacity`: Optional; when supplied, must be from 1 to 10000.
- `location`: Optional, max 150 chars.
- `status`: Required; must be `Active`, `Inactive`, or `Maintenance`.
- Input text is trimmed before validation and persistence.

## Error Cases
- Room code already exists.
- Cannot change a room to `Inactive` or `Maintenance` while an active class uses it.
- Cannot delete a room while an active class uses it.
- Room not found or unauthorized.
