SELECT id AS "Id", equipment_id AS "EquipmentId", revision_date AS "RevisionDate",
       comment AS "Comment", created_at AS "CreatedAt"
FROM equipment_revisions
WHERE equipment_id = @EquipmentId
ORDER BY revision_date DESC;
