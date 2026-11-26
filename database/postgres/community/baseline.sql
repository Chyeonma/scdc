-- Fresh Community database only. Generated from the clean main@7a2a88b schema on PostgreSQL 18.
SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;


INSERT INTO community.permissions (code, description) VALUES ('manage_channels', 'Manage channels');
INSERT INTO community.permissions (code, description) VALUES ('manage_invites', 'Manage invitations');
INSERT INTO community.permissions (code, description) VALUES ('review_join_requests', 'Review join requests');
INSERT INTO community.permissions (code, description) VALUES ('manage_join_mode', 'Manage join mode');
INSERT INTO community.permissions (code, description) VALUES ('manage_channel_access', 'Manage channel access');
INSERT INTO community.permissions (code, description) VALUES ('channel_view', 'View a channel');



INSERT INTO community.schema_migrations (module, version, checksum, applied_at) VALUES ('community', 1, 'b434be5e002497ef204a39c38d7cb4d0783f869804824e130779e74d8f8adab4', clock_timestamp());
INSERT INTO community.schema_migrations (module, version, checksum, applied_at) VALUES ('community', 2, 'fe07dcc516bfce7eedf257c8fb42d7baa615c716aba9034de9f4e9cebe40ccff', clock_timestamp());
INSERT INTO community.schema_migrations (module, version, checksum, applied_at) VALUES ('community', 3, '9c109521897297c26a2f1b920194b8ef725b2828be98d1ffb3134f8896e1069d', clock_timestamp());
INSERT INTO community.schema_migrations (module, version, checksum, applied_at) VALUES ('community', 4, 'b5518a26af4b4987b0c2b8aed09909afdfb6d7884c7133cf78fa330af253206c', clock_timestamp());
INSERT INTO community.schema_migrations (module, version, checksum, applied_at) VALUES ('community', 20261120, '8859651de84d446acd999993684af4ae0db65e2c450f84bfd19e43ce280a406f', clock_timestamp());
INSERT INTO community.schema_migrations (module, version, checksum, applied_at) VALUES ('community', 2026112204, 'b54dd1483652134aea7cddaf50d1054966546bdd7e0c9011a746f75bc58409d0', clock_timestamp());
INSERT INTO community.schema_migrations (module, version, checksum, applied_at) VALUES ('community', 2026112307, 'da14e4af4ddebff8c3344ee33f9be200a2983e53417ac50faf73367dbd4bb4f4', clock_timestamp());
INSERT INTO community.schema_migrations (module, version, checksum, applied_at) VALUES ('community', 2026112409, '19608efaa3f2e240cce01bb023da158ceb3b1039a747314c9d457ec35bbb7d29', clock_timestamp());
