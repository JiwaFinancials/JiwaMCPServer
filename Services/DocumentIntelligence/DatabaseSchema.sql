-- PostgreSQL + pgvector production schema for MCP document intelligence
-- Run: CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS documents (
	document_id TEXT PRIMARY KEY,
	tenant_id TEXT NOT NULL,
	title TEXT NOT NULL,
	file_name TEXT NOT NULL,
	mime_type TEXT NOT NULL,
	file_hash TEXT NOT NULL,
	file_size_bytes BIGINT NOT NULL,
	page_count INT NOT NULL DEFAULT 0,
	document_type TEXT NOT NULL DEFAULT 'Unknown',
	processing_status TEXT NOT NULL,
	processing_error TEXT NULL,
	invoice_number TEXT NULL,
	vendor TEXT NULL,
	invoice_date TEXT NULL,
	total_amount NUMERIC(18, 4) NULL,
	tax_amount NUMERIC(18, 4) NULL,
	currency TEXT NULL,
	metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	UNIQUE (tenant_id, file_hash)
);

CREATE INDEX IF NOT EXISTS ix_documents_tenant_status ON documents (tenant_id, processing_status);
CREATE INDEX IF NOT EXISTS ix_documents_tenant_updated ON documents (tenant_id, updated_at DESC);

CREATE TABLE IF NOT EXISTS document_pages (
	document_id TEXT NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
	tenant_id TEXT NOT NULL,
	page_number INT NOT NULL,
	used_ocr BOOLEAN NOT NULL DEFAULT FALSE,
	text_content TEXT NOT NULL,
	headings JSONB NOT NULL DEFAULT '[]'::jsonb,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	PRIMARY KEY (tenant_id, document_id, page_number)
);

CREATE TABLE IF NOT EXISTS document_tables (
	table_id BIGSERIAL PRIMARY KEY,
	tenant_id TEXT NOT NULL,
	document_id TEXT NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
	page_number INT NOT NULL,
	caption TEXT NOT NULL,
	headers JSONB NOT NULL DEFAULT '[]'::jsonb,
	rows_json JSONB NOT NULL DEFAULT '[]'::jsonb,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_document_tables_doc_page ON document_tables (tenant_id, document_id, page_number);

CREATE TABLE IF NOT EXISTS document_chunks (
	chunk_id TEXT PRIMARY KEY,
	tenant_id TEXT NOT NULL,
	document_id TEXT NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
	start_page INT NOT NULL,
	end_page INT NOT NULL,
	chunk_text TEXT NOT NULL,
	headings JSONB NOT NULL DEFAULT '[]'::jsonb,
	tables_json JSONB NOT NULL DEFAULT '[]'::jsonb,
	metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_document_chunks_doc ON document_chunks (tenant_id, document_id);
CREATE INDEX IF NOT EXISTS ix_document_chunks_pages ON document_chunks (tenant_id, document_id, start_page, end_page);

CREATE TABLE IF NOT EXISTS document_embeddings (
	chunk_id TEXT PRIMARY KEY REFERENCES document_chunks(chunk_id) ON DELETE CASCADE,
	tenant_id TEXT NOT NULL,
	document_id TEXT NOT NULL,
	embedding vector(1536) NOT NULL,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_document_embeddings_tenant_doc ON document_embeddings (tenant_id, document_id);
CREATE INDEX IF NOT EXISTS ix_document_embeddings_vector ON document_embeddings USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);

CREATE TABLE IF NOT EXISTS document_processing_events (
	event_id BIGSERIAL PRIMARY KEY,
	tenant_id TEXT NOT NULL,
	document_id TEXT NOT NULL,
	status TEXT NOT NULL,
	detail TEXT NULL,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_document_processing_events_doc ON document_processing_events (tenant_id, document_id, created_at DESC);
