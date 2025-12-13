CREATE TABLE IF NOT EXISTS file_uploads (
    id UUID PRIMARY KEY,
    file_name VARCHAR(255) NOT NULL,
    file_size BIGINT NOT NULL,
    upload_date TIMESTAMP WITH TIME ZONE NOT NULL,
    student_name VARCHAR(100) NOT NULL,
    student_surname VARCHAR(100) NOT NULL,
    group_number INTEGER NOT NULL,
    assignment_id UUID NOT NULL,

    CONSTRAINT idx_assignment_id UNIQUE (assignment_id, id)
);

CREATE INDEX IF NOT EXISTS idx_file_uploads_assignment 
ON file_uploads(assignment_id);

CREATE INDEX IF NOT EXISTS idx_file_uploads_date 
ON file_uploads(upload_date DESC);

COMMENT ON TABLE file_uploads IS 'Метаданные загруженных файлов студентов';
COMMENT ON COLUMN file_uploads.id IS 'Уникальный идентификатор файла';
COMMENT ON COLUMN file_uploads.file_name IS 'Имя файла';
COMMENT ON COLUMN file_uploads.file_size IS 'Размер файла в байтах';
COMMENT ON COLUMN file_uploads.upload_date IS 'Дата и время загрузки';
COMMENT ON COLUMN file_uploads.student_name IS 'Имя студента';
COMMENT ON COLUMN file_uploads.student_surname IS 'Фамилия студента';
COMMENT ON COLUMN file_uploads.group_number IS 'Номер группы';
COMMENT ON COLUMN file_uploads.assignment_id IS 'Идентификатор задания';

CREATE TABLE IF NOT EXISTS analysis_reports (
    id UUID PRIMARY KEY,
    file_id UUID NOT NULL,
    is_plagiarized BOOLEAN NOT NULL,
    similarity_percentage DOUBLE PRECISION NOT NULL,
    analysis_date TIMESTAMP WITH TIME ZONE NOT NULL,
    report_file_path VARCHAR(500),

    CONSTRAINT fk_analysis_reports_file_id 
        FOREIGN KEY (file_id) 
        REFERENCES file_uploads(id) 
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_analysis_reports_file_id 
ON analysis_reports(file_id);

CREATE INDEX IF NOT EXISTS idx_analysis_reports_date 
ON analysis_reports(analysis_date DESC);

COMMENT ON TABLE analysis_reports IS 'Отчеты анализа файлов на плагиат';
COMMENT ON COLUMN analysis_reports.id IS 'Уникальный идентификатор отчета';
COMMENT ON COLUMN analysis_reports.file_id IS 'Идентификатор проанализированного файла';
COMMENT ON COLUMN analysis_reports.is_plagiarized IS 'Флаг наличия плагиата';
COMMENT ON COLUMN analysis_reports.similarity_percentage IS 'Процент схожести (0-100)';
COMMENT ON COLUMN analysis_reports.analysis_date IS 'Дата и время анализа';
COMMENT ON COLUMN analysis_reports.report_file_path IS 'Путь к файлу отчета';