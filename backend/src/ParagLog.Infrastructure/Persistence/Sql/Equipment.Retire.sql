UPDATE equipment
SET retired = true
WHERE id = @EquipmentId AND user_id = @UserId;
