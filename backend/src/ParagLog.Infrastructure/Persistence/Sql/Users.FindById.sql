SELECT id AS "Id", public_id AS "PublicId", username::text AS "Username", email::text AS "Email",
       password_hash AS "PasswordHash", google_sub AS "GoogleSub", created_at AS "CreatedAt"
FROM users
WHERE id = @UserId;
