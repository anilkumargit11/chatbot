import CloudUploadOutlinedIcon from '@mui/icons-material/CloudUploadOutlined';
import { Box, Button, LinearProgress, Stack, Typography, alpha } from '@mui/material';
import { useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import { toast } from 'react-toastify';

const allowedTypes = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'text/plain'
];

type DocumentDropzoneProps = {
  disabled?: boolean;
  progress: number;
  onUpload: (file: File) => Promise<void>;
};

export function DocumentDropzone({ disabled, progress, onUpload }: DocumentDropzoneProps) {
  const onDrop = useCallback(
    async (acceptedFiles: File[]) => {
      const file = acceptedFiles[0];

      if (!file) {
        return;
      }

      if (!allowedTypes.includes(file.type)) {
        toast.error('Only PDF, DOCX, and TXT files are supported.');
        return;
      }

      await onUpload(file);
    },
    [onUpload]
  );

  const { getRootProps, getInputProps, isDragActive, open } = useDropzone({
    onDrop,
    multiple: false,
    noClick: true,
    disabled,
    accept: {
      'application/pdf': ['.pdf'],
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'],
      'text/plain': ['.txt']
    }
  });

  return (
    <Box
      {...getRootProps()}
      sx={(theme) => ({
        border: '1px dashed',
        borderColor: isDragActive ? 'primary.main' : 'divider',
        bgcolor: isDragActive ? alpha(theme.palette.primary.main, 0.08) : 'background.paper',
        borderRadius: 2,
        p: { xs: 3, md: 5 },
        textAlign: 'center'
      })}
    >
      <input {...getInputProps()} />
      <Stack alignItems="center" spacing={2}>
        <CloudUploadOutlinedIcon color="primary" sx={{ fontSize: 56 }} />
        <Box>
          <Typography variant="h6">Drop files here</Typography>
          <Typography color="text.secondary">PDF, DOCX, or TXT documents up to your API limits.</Typography>
        </Box>
        <Button disabled={disabled} onClick={open} variant="contained">
          Browse files
        </Button>
        {disabled && (
          <Box sx={{ width: '100%', maxWidth: 420 }}>
            <LinearProgress value={progress} variant="determinate" />
            <Typography color="text.secondary" sx={{ mt: 1 }} variant="body2">
              Uploading {progress}%
            </Typography>
          </Box>
        )}
      </Stack>
    </Box>
  );
}
