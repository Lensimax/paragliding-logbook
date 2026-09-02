SELECT EXISTS(
    SELECT 1 FROM equipment
    WHERE user_id = @UserId AND display_name = @DisplayName
      AND (@ExcludingId::uuid IS NULL OR id <> @ExcludingId::uuid)
);
