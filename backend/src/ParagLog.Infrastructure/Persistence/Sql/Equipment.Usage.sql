SELECT COUNT(ae.activity_id)::int AS "ActivityCount",
       (COALESCE(SUM(a.duration_seconds), 0) / 3600.0)::float8 AS "Hours"
FROM equipment e
LEFT JOIN activity_equipment ae ON ae.equipment_id = e.id
LEFT JOIN activities a ON a.id = ae.activity_id
WHERE e.id = @EquipmentId AND e.user_id = @UserId
GROUP BY e.id;
