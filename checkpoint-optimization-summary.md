# Checkpoint System Optimization Summary

This document summarizes the implementation of the checkpoint system optimization that adds `checkpoints_ready` and `checkpoints_reserved` tables to reduce hot spot contention on the main checkpoints table.

## Overview

The checkpoints table was identified as a hot spot in the system. To optimize performance, we've added two new tables:
- `checkpoints_ready` (id, process_at) - tracks checkpoints ready for processing
- `checkpoints_reserved` (id, reserved_until) - tracks currently reserved checkpoints

## Migration (`db/versions/0.23.0.sql`)

### New Tables Created
- **`checkpoints_ready`** with `id` and `process_at` columns
- **`checkpoints_reserved`** with `id` and `reserved_until` columns
- **Foreign key constraints** linking both tables to the main checkpoints table
- **Performance indexes** on `process_at` and `reserved_until`

### Data Migration
- **Populated new tables** with existing ready/reserved checkpoints from main table
- **Updated trigger function** to automatically manage the new tables during checkpoint lifecycle

## Query Updates

### Core Subscription Queries

#### `RecordCheckpoints.cs`
- Updated to insert into `checkpoints_ready` when global consumer updates checkpoints
- Uses CTE to handle both main table update and ready table insertion

#### `ReserveNextAvailableCheckpoint.cs` 
- Completely rewritten to use atomic CTE transaction:
  1. DELETE from `checkpoints_ready` (with group filtering and FOR UPDATE SKIP LOCKED)
  2. UPDATE main `checkpoints` table to set `reserved_until`
  3. INSERT into `checkpoints_reserved`
- Ensures atomic reservation with no race conditions

#### `ReleaseCheckpointReservation.cs`
- Added deletion from `checkpoints_reserved` table
- Maintains cleanup of reservation state

#### `RecoverExpiredCheckpointReservations.cs`
- Rewritten to query `checkpoints_reserved` for expired reservations
- Uses CTE to atomically:
  1. Find expired reservations from `checkpoints_reserved`
  2. DELETE from `checkpoints_reserved` 
  3. UPDATE main checkpoints table
  4. INSERT back into `checkpoints_ready` if still ready

#### `UpdateCheckpointPosition.cs`
- Updated to manage both new tables when checkpoint processing completes
- Removes from `checkpoints_reserved` and conditionally adds to `checkpoints_ready`

#### `RecordCheckpointError.cs`
- Updated to handle reservation cleanup on error
- Inserts into `checkpoints_ready` for retry attempts

#### `AdvanceLaggingSubscriptionCheckpoints.cs`
- Updated to remove from `checkpoints_ready` when advancing lagging checkpoints
- Ensures ready table stays in sync

### Dashboard Query Updates

#### `ReservationsQuery.cs`
- Updated to query from `checkpoints_reserved` table instead of main table
- Improved performance by avoiding scan of full checkpoints table

#### `SkipQuery.cs` & `BulkSkipQuery.cs`
- Added cleanup of both `checkpoints_ready` and `checkpoints_reserved` tables
- Ensures consistent state when manually skipping checkpoints

#### `ScheduleCheckpoints.cs`
- Updated to insert into `checkpoints_ready` when scheduling checkpoints
- Uses UPSERT pattern for idempotent scheduling

## Trigger Function Updates

The `checkpoint_preprocessor()` function was enhanced to automatically manage the new tables:

### Ready State Management
- Automatically inserts into `checkpoints_ready` when checkpoints become ready
- Removes from `checkpoints_ready` when no longer ready
- Handles both INSERT and UPDATE operations

### Reservation Management  
- Moves checkpoints between `ready` and `reserved` tables based on `reserved_until` changes
- Handles reservation creation, updates, and releases
- Ensures referential integrity between tables

## Key Benefits

1. **Reduced Hot Spot Contention**: Main checkpoints table no longer used for reservation queries
2. **Atomic Reservation Operations**: CTE-based DELETE/INSERT pattern eliminates race conditions  
3. **Improved Query Performance**: Smaller, focused tables with targeted indexes
4. **Automatic State Management**: Trigger function maintains consistency across tables
5. **Backward Compatibility**: All existing functionality preserved through main checkpoints table
6. **Better Scalability**: Reservation operations scale independently of total checkpoint count

## Usage Pattern

1. **Global Consumer**: Updates main checkpoints table, trigger automatically populates `checkpoints_ready`
2. **Checkpoint Consumer**: Queries `checkpoints_reserved` by group, reserves via atomic CTE operation  
3. **Processing Complete**: Updates position, automatically manages table transitions
4. **Error Handling**: Releases reservations and re-queues for retry as needed
5. **Dashboard**: Uses optimized queries against focused tables for better performance

The optimization maintains all existing behavior while significantly improving performance characteristics for checkpoint reservation and processing workflows.