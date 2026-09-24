-- Apply once to existing installations; schema.sql covers fresh databases.
INSERT INTO community.permissions (code, description) VALUES
    ('message.edit_own', 'Edit own messages in a channel'),
    ('message.delete', 'Delete other users messages in a channel')
ON CONFLICT (code) DO NOTHING;
