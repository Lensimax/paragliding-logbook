SELECT id AS "Id", user_id AS "UserId", display_name AS "DisplayName", type::text AS "Type",
       brand AS "Brand", model AS "Model", purchase_date AS "PurchaseDate",
       next_revision_date AS "NextRevisionDate", auto_add AS "AutoAdd", retired AS "Retired",
       created_at AS "CreatedAt"
FROM equipment
WHERE user_id = @UserId
ORDER BY created_at;
