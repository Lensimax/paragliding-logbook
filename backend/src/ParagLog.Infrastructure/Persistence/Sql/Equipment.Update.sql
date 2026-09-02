UPDATE equipment
SET display_name = @DisplayName,
    type = @Type::equipment_type,
    brand = @Brand,
    model = @Model,
    purchase_date = @PurchaseDate,
    next_revision_date = @NextRevisionDate,
    auto_add = @AutoAdd
WHERE id = @Id AND user_id = @UserId;
