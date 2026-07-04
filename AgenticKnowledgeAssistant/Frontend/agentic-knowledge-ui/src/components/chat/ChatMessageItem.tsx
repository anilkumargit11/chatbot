import ContentCopyOutlinedIcon from '@mui/icons-material/ContentCopyOutlined';
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import IosShareOutlinedIcon from '@mui/icons-material/IosShareOutlined';
import RefreshOutlinedIcon from '@mui/icons-material/RefreshOutlined';
import ThumbDownAltOutlinedIcon from '@mui/icons-material/ThumbDownAltOutlined';
import ThumbUpAltOutlinedIcon from '@mui/icons-material/ThumbUpAltOutlined';
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined';
import PsychologyOutlinedIcon from '@mui/icons-material/PsychologyOutlined';
import VolumeUpOutlinedIcon from '@mui/icons-material/VolumeUpOutlined';
import AttachFileIcon from '@mui/icons-material/AttachFile';
import TranslateIcon from '@mui/icons-material/Translate';
import { Avatar, Box, Chip, Fade, IconButton, Paper, Stack, Tooltip, Typography, alpha, Button } from '@mui/material';
import { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import { toast } from 'react-toastify';
import rehypeHighlight from 'rehype-highlight';
import remarkGfm from 'remark-gfm';
import { ChatMessage } from '../../models/api';

type ChatMessageItemProps = {
  message: ChatMessage;
  onEdit?: (message: ChatMessage) => void;
  onRegenerate?: (message: ChatMessage) => void;
};

export function ChatMessageItem({ message, onEdit, onRegenerate }: ChatMessageItemProps) {
  const isUser = message.role === 'user';
  const [showOriginal, setShowOriginal] = useState(false);

  async function copyMessage() {
    await navigator.clipboard.writeText(message.content);
    toast.success(isUser ? 'Question copied' : 'Answer copied');
  }

  async function shareMessage() {
    if (navigator.share) {
      await navigator.share({ text: message.content, title: isUser ? 'Question' : 'Assistant answer' });
      return;
    }

    await copyMessage();
  }

  function exportAnswer(extension: 'md' | 'doc') {
    const blob = new Blob([message.content], {
      type: extension === 'doc' ? 'application/msword' : 'text/markdown;charset=utf-8'
    });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `assistant-answer-${new Date(message.createdAt).getTime()}.${extension}`;
    anchor.click();
    URL.revokeObjectURL(url);
    toast.success(extension === 'doc' ? 'Word file downloaded' : 'Markdown file downloaded');
  }

  function rate(label: string) {
    toast.success(`${label} saved`);
  }

  function speak() {
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
      const textToSpeak = message.translatedAnswer && !showOriginal ? message.translatedAnswer : message.content;
      const utterance = new SpeechSynthesisUtterance(textToSpeak);
      const lang = message.detectedLanguage || 'en';
      if (lang === 'te') utterance.lang = 'te-IN';
      else if (lang === 'hi') utterance.lang = 'hi-IN';
      else if (lang === 'ta') utterance.lang = 'ta-IN';
      else if (lang === 'kn') utterance.lang = 'kn-IN';
      else utterance.lang = 'en-US';
      window.speechSynthesis.speak(utterance);
      toast.success('Reading answer aloud...');
    } else {
      toast.error('Text-to-speech not supported in this browser.');
    }
  }

  return (
    <Fade in timeout={220}>
      <Stack
        className="chat-message-row"
        direction="row"
        justifyContent={isUser ? 'flex-end' : 'flex-start'}
        spacing={1.5}
        sx={{ position: 'relative' }}
      >
      {!isUser && (
        <Avatar sx={{ bgcolor: 'secondary.main', mt: 0.5, width: 34, height: 34 }}>
          <PsychologyOutlinedIcon />
        </Avatar>
      )}
      <Paper
        elevation={0}
        sx={(theme) => ({
          width: 'fit-content',
          maxWidth: { xs: '88%', md: isUser ? '72%' : '78%' },
          p: { xs: 1.5, md: 2 },
          border: 1,
          borderColor: message.isError
            ? 'error.main'
            : isUser
              ? alpha(theme.palette.primary.main, 0.2)
              : alpha(theme.palette.divider, 0.8),
          borderRadius: isUser ? '18px 18px 6px 18px' : '18px 18px 18px 6px',
          bgcolor: isUser
            ? alpha(theme.palette.primary.main, theme.palette.mode === 'dark' ? 0.22 : 0.1)
            : theme.palette.background.paper,
          boxShadow: theme.palette.mode === 'dark'
            ? '0 16px 36px rgba(0,0,0,0.18)'
            : '0 18px 40px rgba(15,23,42,0.06)'
        })}
      >
        <Stack spacing={1.25}>
          <Box
            sx={{
              overflowX: 'auto',
              '& h1, & h2, & h3': {
                mt: 0,
                mb: 1,
                fontWeight: 800,
                lineHeight: 1.25
              },
              '& h1': { fontSize: '1.35rem' },
              '& h2': { fontSize: '1.15rem' },
              '& h3': { fontSize: '1rem', mt: 2 },
              '& p': { m: 0, lineHeight: 1.7 },
              '& p + p': { mt: 1 },
              '& ul, & ol': { mt: 1, mb: 1, pl: 3 },
              '& li': { mb: 0.5, lineHeight: 1.65 },
              '& pre': {
                my: 1.5,
                borderRadius: 1,
                border: '1px solid',
                borderColor: 'divider',
                bgcolor: '#111827',
                color: 'grey.100',
                fontSize: '0.86rem'
              },
              '& code:not(pre code)': {
                px: 0.5,
                py: 0.15,
                borderRadius: 1,
                bgcolor: 'action.hover'
              },
              '& table': {
                width: '100%',
                borderCollapse: 'collapse',
                my: 1.5,
                fontSize: '0.875rem'
              },
              '& th, & td': {
                border: '1px solid',
                borderColor: 'divider',
                px: 1.25,
                py: 0.85,
                textAlign: 'left',
                verticalAlign: 'top'
              },
              '& th': {
                bgcolor: 'action.hover',
                fontWeight: 800
              },
              '& blockquote': {
                m: 0,
                my: 1.5,
                pl: 2,
                borderLeft: '4px solid',
                borderColor: 'primary.main',
                color: 'text.secondary'
              }
            }}
          >
            {/* Attachment preview for User message */}
            {isUser && message.attachmentName && (
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1, p: 1, borderRadius: 2, bgcolor: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)' }}>
                <AttachFileIcon sx={{ fontSize: 16 }} />
                <Typography variant="caption" fontWeight={700} noWrap>
                  {message.attachmentName}
                </Typography>
              </Stack>
            )}

            {/* Translated vs Original Answer Toggle */}
            {message.translatedAnswer && (
              <Stack direction="row" spacing={1} sx={{ mb: 1.5 }}>
                <Button
                  size="small"
                  variant="outlined"
                  startIcon={<TranslateIcon />}
                  onClick={() => setShowOriginal(prev => !prev)}
                  sx={{ py: 0.25, px: 1, minHeight: 28, fontSize: '0.75rem', borderRadius: 2 }}
                >
                  Show {showOriginal ? 'Translation' : 'Original English'}
                </Button>
              </Stack>
            )}

            <ReactMarkdown rehypePlugins={[rehypeHighlight]} remarkPlugins={[remarkGfm]}>
              {message.translatedAnswer && !showOriginal ? message.translatedAnswer : message.content}
            </ReactMarkdown>
          </Box>
          <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1}>
            <Stack direction="row" flexWrap="wrap" gap={0.75}>
              {message.elapsedMs && <Chip label={`${(message.elapsedMs / 1000).toFixed(2)}s`} size="small" />}
              <Typography className="message-timestamp" color="text.secondary" variant="caption">
                {new Date(message.createdAt).toLocaleTimeString()}
              </Typography>
            </Stack>
            <Stack className="message-actions" direction="row" spacing={0.25}>
              <Tooltip title={isUser ? 'Copy question' : 'Copy answer'}>
                <IconButton aria-label={isUser ? 'Copy question' : 'Copy answer'} onClick={copyMessage} size="small">
                  <ContentCopyOutlinedIcon fontSize="small" />
                </IconButton>
              </Tooltip>
              {isUser && onEdit && (
                <Tooltip title="Edit question">
                  <IconButton aria-label="Edit question" onClick={() => onEdit(message)} size="small">
                    <EditOutlinedIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
              {!isUser && (
                <>
                  <Tooltip title="Read answer aloud">
                    <IconButton aria-label="Read answer aloud" onClick={speak} size="small">
                      <VolumeUpOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  {onRegenerate && (
                    <Tooltip title="Regenerate answer">
                      <IconButton aria-label="Regenerate answer" onClick={() => onRegenerate(message)} size="small">
                        <RefreshOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                  <Tooltip title="Like">
                    <IconButton aria-label="Like answer" onClick={() => rate('Like')} size="small">
                      <ThumbUpAltOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Dislike">
                    <IconButton aria-label="Dislike answer" onClick={() => rate('Dislike')} size="small">
                      <ThumbDownAltOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Share">
                    <IconButton aria-label="Share answer" onClick={() => void shareMessage()} size="small">
                      <IosShareOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Download as Word">
                    <IconButton aria-label="Download answer" onClick={() => exportAnswer('doc')} size="small">
                      <DownloadOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </>
              )}
            </Stack>
          </Stack>
          {!!message.sources?.length && (
            <Stack direction="row" flexWrap="wrap" gap={0.75}>
              {message.sources.map((source) => (
                <Chip key={source} label={source} size="small" variant="outlined" />
              ))}
            </Stack>
          )}
        </Stack>
      </Paper>
      {isUser && (
        <Avatar sx={{ bgcolor: 'primary.main', mt: 0.5, width: 34, height: 34 }}>
          <PersonOutlineOutlinedIcon />
        </Avatar>
      )}
      </Stack>
    </Fade>
  );
}

