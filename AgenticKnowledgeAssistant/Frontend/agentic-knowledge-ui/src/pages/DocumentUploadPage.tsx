import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined';
import RefreshOutlinedIcon from '@mui/icons-material/RefreshOutlined';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';
import { Alert, Box, Card, CardContent, Chip, IconButton, Stack, Tooltip, Typography } from '@mui/material';
import { useCallback, useEffect, useState } from 'react';
import { toast } from 'react-toastify';
import { EmptyState } from '../components/common/EmptyState';
import { PageHeader } from '../components/common/PageHeader';
import { DocumentDropzone } from '../components/upload/DocumentDropzone';
import { DocumentSummary } from '../models/api';
import { documentApi } from '../services/documentApi';

export function DocumentUploadPage() {
  const [documents, setDocuments] = useState<DocumentSummary[]>([]);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);

  const loadDocuments = useCallback(async () => {
    try {
      setDocuments(await documentApi.list());
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to load documents.');
    }
  }, []);

  useEffect(() => {
    void loadDocuments();
  }, [loadDocuments]);

  async function uploadDocument(file: File) {
    try {
      setUploading(true);
      setProgress(0);
      const response = await documentApi.upload(file, setProgress);
      toast.success(response.message);
      await loadDocuments();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Upload failed.');
    } finally {
      setUploading(false);
      setProgress(0);
    }
  }

  async function deleteDocument(id: number) {
    try {
      await documentApi.remove(id);
      toast.success('Document deleted');
      await loadDocuments();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Delete failed.');
    }
  }

  return (
    <Box>
      <PageHeader
        eyebrow="Documents"
        title="Knowledge source indexing"
        description="Permanently ingest PDF, DOCX, and TXT files into the enterprise knowledge base for future RAG searches. For one-time analysis, attach files directly in Chat."
      />
      <Stack spacing={3}>
        <DocumentDropzone disabled={uploading} onUpload={uploadDocument} progress={progress} />
        <Card>
          <CardContent>
            <Stack alignItems="center" direction="row" justifyContent="space-between" sx={{ mb: 2 }}>
              <Typography variant="h6">Indexed Knowledge Documents</Typography>
              <Tooltip title="Refresh documents">
                <IconButton aria-label="Refresh documents" onClick={() => void loadDocuments()}>
                  <RefreshOutlinedIcon />
                </IconButton>
              </Tooltip>
            </Stack>
            <Stack spacing={1.5}>
              {documents.map((document) => (
                <DocumentListItem document={document} key={document.id} onDelete={deleteDocument} />
              ))}
              {!documents.length && <EmptyState title="No indexed documents" description="Drag a supported file into the upload area to add it to the enterprise knowledge base." />}
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    </Box>
  );
}

function DocumentListItem({ document, onDelete }: { document: DocumentSummary; onDelete: (id: number) => Promise<void> }) {
  const hasExtractedText = !document.preview.startsWith('%PDF-') && !document.preview.includes('/Producer');

  return (
    <Stack spacing={1} sx={{ border: 1, borderColor: hasExtractedText ? 'divider' : 'warning.main', borderRadius: 2, p: 2 }}>
      <Stack alignItems={{ xs: 'flex-start', sm: 'center' }} direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={1.5}>
        <Box sx={{ minWidth: 0 }}>
          <Typography fontWeight={800}>{document.title}</Typography>
          <Typography color="text.secondary" noWrap variant="body2">
            {document.preview || 'No preview available'}
          </Typography>
          <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
            <Chip label={new Date(document.createdDate).toLocaleDateString()} size="small" />
            <Chip color={hasExtractedText ? 'success' : 'warning'} label={hasExtractedText ? 'Searchable' : 'Needs re-upload'} size="small" />
          </Stack>
        </Box>
        <Tooltip title="Delete document">
          <IconButton aria-label={`Delete ${document.title}`} color="error" onClick={() => void onDelete(document.id)}>
            <DeleteOutlineOutlinedIcon />
          </IconButton>
        </Tooltip>
      </Stack>
      {!hasExtractedText && (
        <Alert icon={<WarningAmberOutlinedIcon fontSize="inherit" />} severity="warning">
          This file was uploaded before PDF text extraction was enabled. Delete it and upload it again so chat can answer from its BRD content.
        </Alert>
      )}
    </Stack>
  );
}
