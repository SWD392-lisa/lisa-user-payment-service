-- Seed Roles (if not exists) - LUCY, PRO, SUPER are already in your DB, but just in case
INSERT INTO role (role_code, role_name, created_at, updated_at)
VALUES 
    ('LUCY', 'Lucy', NOW(), NOW()),
    ('PRO', 'Pro', NOW(), NOW()),
    ('SUPER', 'Super', NOW(), NOW())
ON CONFLICT (role_code) DO NOTHING;

-- Seed Transaction Types (if not exists)
INSERT INTO transaction_type (code, name, description, is_active, created_at, updated_at)
VALUES 
    ('ONLINE_SEPAY', 'SePay Online Payment', 'Payment made online through the SePay gateway', true, NOW(), NOW()),
    ('ROLE_UPGRADE_SEPAY', 'SePay Role Upgrade', 'Role upgrade payment through the SePay gateway', true, NOW(), NOW()),
    ('GIFT_SEND', 'Gift Sent', 'Debit for sending a virtual gift', true, NOW(), NOW()),
    ('GIFT_RECEIVE', 'Gift Received', 'Credit for receiving a virtual gift', true, NOW(), NOW())
ON CONFLICT (code) DO NOTHING;

-- Seed Sample Gifts (if not exists)
INSERT INTO gift_catalog (id, name, description, icon_url, price, currency, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'Heart', 'A small red heart', 'https://example.com/icons/heart.png', 1000, 'VND', true, NOW(), NOW()),
    (gen_random_uuid(), 'Flower', 'A beautiful bouquet', 'https://example.com/icons/flower.png', 5000, 'VND', true, NOW(), NOW()),
    (gen_random_uuid(), 'Star', 'A shining golden star', 'https://example.com/icons/star.png', 10000, 'VND', true, NOW(), NOW()),
    (gen_random_uuid(), 'Crown', 'A majestic crown', 'https://example.com/icons/crown.png', 50000, 'VND', true, NOW(), NOW())
ON CONFLICT DO NOTHING;
