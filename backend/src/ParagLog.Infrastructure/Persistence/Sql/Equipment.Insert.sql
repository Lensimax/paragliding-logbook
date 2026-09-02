INSERT INTO equipment (
    id, user_id, display_name, type, brand, model, purchase_date, next_revision_date,
    auto_add, retired, created_at
) VALUES (
    @Id, @UserId, @DisplayName, @Type::equipment_type, @Brand, @Model, @PurchaseDate, @NextRevisionDate,
    @AutoAdd, false, @CreatedAt
);
