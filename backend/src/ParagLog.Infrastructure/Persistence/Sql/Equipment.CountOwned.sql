SELECT COUNT(*) FROM equipment
WHERE user_id = @UserId AND id = ANY(@EquipmentIds);
