SELECT id AS "Id", user_id AS "UserId", token_hash AS "TokenHash", created_at AS "CreatedAt",
       expires_at AS "ExpiresAt", persistent AS "Persistent", user_agent AS "UserAgent", ip_hash AS "IpHash"
FROM sessions
WHERE token_hash = @TokenHash;
