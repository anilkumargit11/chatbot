import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined';
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import PsychologyOutlinedIcon from '@mui/icons-material/PsychologyOutlined';
import RefreshOutlinedIcon from '@mui/icons-material/RefreshOutlined';
import SendOutlinedIcon from '@mui/icons-material/SendOutlined';
import AttachFileIcon from '@mui/icons-material/AttachFile';
import ImageOutlinedIcon from '@mui/icons-material/ImageOutlined';
import PhotoCameraOutlinedIcon from '@mui/icons-material/PhotoCameraOutlined';
import MicIcon from '@mui/icons-material/Mic';
import MicOffIcon from '@mui/icons-material/MicOff';
import StopCircleOutlinedIcon from '@mui/icons-material/StopCircleOutlined';
import PreviewIcon from '@mui/icons-material/Preview';
import TranslateIcon from '@mui/icons-material/Translate';
import CloseIcon from '@mui/icons-material/Close';
import InsertDriveFileOutlinedIcon from '@mui/icons-material/InsertDriveFileOutlined';
import TableChartOutlinedIcon from '@mui/icons-material/TableChartOutlined';
import SlideshowOutlinedIcon from '@mui/icons-material/SlideshowOutlined';
import ArchiveOutlinedIcon from '@mui/icons-material/ArchiveOutlined';
import AudioFileOutlinedIcon from '@mui/icons-material/AudioFileOutlined';
import VideoFileOutlinedIcon from '@mui/icons-material/VideoFileOutlined';
import AddCommentOutlinedIcon from '@mui/icons-material/AddCommentOutlined';
import PushPinOutlinedIcon from '@mui/icons-material/PushPinOutlined';
import StarBorderOutlinedIcon from '@mui/icons-material/StarBorderOutlined';
import StarOutlinedIcon from '@mui/icons-material/StarOutlined';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  IconButton,
  InputBase,
  Paper,
  Stack,
  Tooltip,
  Typography,
  alpha,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  useTheme,
  Grid
} from '@mui/material';
import { FormEvent, useEffect, useRef, useState } from 'react';
import { toast } from 'react-toastify';
import { ChatMessageItem } from '../components/chat/ChatMessageItem';
import { TypingIndicator } from '../components/chat/TypingIndicator';
import { PageHeader } from '../components/common/PageHeader';
import { ChatMessage, ConversationSession } from '../models/api';
import { chatApi, normalizeLegacyAssistantText } from '../services/chatApi';
import { historyApi } from '../services/historyApi';

const welcomeMessage: ChatMessage = {
  id: 'welcome',
  role: 'assistant',
  content: 'Welcome to your premium Enterprise AI Assistant. Access document knowledge, OCR scanned resources, write database queries, or ask general questions.',
  createdAt: new Date().toISOString()
};

const quickPrompts = [
  'How many BRD documents have u?',
  'List uploaded documents',
  'Show stored procedures',
  'Code review index optimization'
];

const aiModes = [
  { value: 'SmartAuto', label: 'Smart Auto' },
  { value: 'Normal', label: 'General AI' },
  { value: 'Enterprise', label: 'Enterprise Search 🔍' },
  { value: 'Database', label: 'SQL / DB Assistant 🗄️' },
  { value: 'Developer', label: 'Developer Mode 💻' },
  { value: 'Ocr', label: 'Document OCR Mode 📑' },
  { value: 'Vision', label: 'Vision AI Mode 👁️' },
  { value: 'Translate', label: 'Translation Mode 🌐' },
  { value: 'Summarize', label: 'Summarization Mode 📋' }
];

import { motion } from 'framer-motion';

const languages = [
  { code: 'en', name: 'English 🇺🇸' },
  { code: 'te', name: 'Telugu 🇮🇳' },
  { code: 'hi', name: 'Hindi 🇮🇳' },
  { code: 'ta', name: 'Tamil 🇮🇳' },
  { code: 'kn', name: 'Kannada 🇮🇳' }
];

const promptTemplates = [
  { label: 'Solid Code Reviewer 💻', text: 'Perform a comprehensive code review of this function. Analyze bugs, SOLID principles compliance, and performance bottlenecks.' },
  { label: 'Database Index Optimizer 🗄️', text: 'Audit this SQL query and suggest index definitions and query optimizations to speed up latency.' },
  { label: 'Technical Report Summary 📋', text: 'Summarize the attached document, list all critical milestones, and outline the action items.' },
  { label: 'Prescription Medical Audit 📑', text: 'Analyze this medical prescription: identify the drugs, dosages, potential contraindications, and usage instructions.' }
];

type PendingAttachment = {
  id: string;
  fileName: string;
  contentType: string;
  base64Content: string;
  size: number;
  previewUrl?: string;
  kind: 'image' | 'pdf' | 'document' | 'spreadsheet' | 'presentation' | 'archive' | 'audio' | 'video' | 'text' | 'code' | 'other';
  progress: number;
  status: 'uploading' | 'ready' | 'failed';
  metadata: string[];
};

const documentAccept = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/msword',
  'text/plain',
  'text/markdown',
  'text/csv',
  'application/vnd.ms-excel',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'application/vnd.ms-powerpoint',
  'application/vnd.openxmlformats-officedocument.presentationml.presentation',
  'application/zip',
  '.md',
  '.sql'
].join(',');

const imageAccept = 'image/jpeg,image/png,image/bmp,image/gif,image/webp,image/heic,image/heif';

function detectAttachmentKind(file: File): PendingAttachment['kind'] {
  const name = file.name.toLowerCase();
  const type = file.type.toLowerCase();

  if (type.startsWith('image/') || /\.(jpg|jpeg|png|bmp|gif|webp|heic|heif)$/.test(name)) return 'image';
  if (type === 'application/pdf' || name.endsWith('.pdf')) return 'pdf';
  if (/\.(xlsx|xls|csv)$/.test(name)) return 'spreadsheet';
  if (/\.(pptx|ppt)$/.test(name)) return 'presentation';
  if (/\.(docx|doc)$/.test(name)) return 'document';
  if (/\.(zip|rar|7z)$/.test(name)) return 'archive';
  if (type.startsWith('audio/') || /\.(mp3|wav|m4a|ogg)$/.test(name)) return 'audio';
  if (type.startsWith('video/') || /\.(mp4|mov|avi|webm)$/.test(name)) return 'video';
  if (/\.(sql|cs|js|ts|tsx|json|xml|yaml|yml)$/.test(name)) return 'code';
  if (/\.(txt|md)$/.test(name)) return 'text';
  return 'other';
}

function inferModeFromAttachments(items: PendingAttachment[]) {
  if (items.some((item) => item.kind === 'image' || item.kind === 'pdf')) return 'Vision';
  if (items.some((item) => item.kind === 'code')) return 'Developer';
  if (items.some((item) => item.kind === 'spreadsheet' || item.kind === 'presentation' || item.kind === 'document')) return 'SmartAuto';
  return 'SmartAuto';
}

function statusLabel(kind: PendingAttachment['kind']) {
  const labels: Record<PendingAttachment['kind'], string> = {
    image: 'Vision AI ready',
    pdf: 'OCR ready',
    document: 'Document parser ready',
    spreadsheet: 'Spreadsheet analyzer ready',
    presentation: 'Slide analyzer ready',
    archive: 'Archive attached',
    audio: 'Speech analysis ready',
    video: 'Video OCR ready',
    text: 'Text parser ready',
    code: 'Code assistant ready',
    other: 'Attachment ready'
  };

  return labels[kind];
}

function formatFileSize(size: number) {
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${(size / 1024 / 1024).toFixed(1)} MB`;
}

function readFileAsDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}

function readImageDimensions(src: string): Promise<{ width: number; height: number }> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve({ width: image.naturalWidth, height: image.naturalHeight });
    image.onerror = reject;
    image.src = src;
  });
}

function guessContentType(fileName: string) {
  const extension = fileName.split('.').pop()?.toLowerCase();
  const map: Record<string, string> = {
    pdf: 'application/pdf',
    docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    pptx: 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
    csv: 'text/csv',
    md: 'text/markdown',
    txt: 'text/plain',
    sql: 'text/plain',
    zip: 'application/zip'
  };

  return extension ? map[extension] ?? 'application/octet-stream' : 'application/octet-stream';
}

function AttachmentKindIcon({ kind }: { kind: PendingAttachment['kind'] }) {
  const sx = { fontSize: 20 };
  if (kind === 'image') return <ImageOutlinedIcon sx={sx} />;
  if (kind === 'spreadsheet') return <TableChartOutlinedIcon sx={sx} />;
  if (kind === 'presentation') return <SlideshowOutlinedIcon sx={sx} />;
  if (kind === 'archive') return <ArchiveOutlinedIcon sx={sx} />;
  if (kind === 'audio') return <AudioFileOutlinedIcon sx={sx} />;
  if (kind === 'video') return <VideoFileOutlinedIcon sx={sx} />;
  return <InsertDriveFileOutlinedIcon sx={sx} />;
}

export function ChatAssistantPage() {
  const theme = useTheme();
  const streamingIntervalRef = useRef<any>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([welcomeMessage]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [editingMessageId, setEditingMessageId] = useState<string | null>(null);
  const [sessionGuid, setSessionGuid] = useState<string>(() => crypto.randomUUID());
  const [conversations, setConversations] = useState<ConversationSession[]>([]);
  const [conversationSearch, setConversationSearch] = useState('');
  const [aiMode, setAiMode] = useState('SmartAuto');
  const [targetLang, setTargetLang] = useState('en');
  
  const [attachments, setAttachments] = useState<PendingAttachment[]>([]);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [isDraggingFiles, setIsDraggingFiles] = useState(false);
  const documentInputRef = useRef<HTMLInputElement | null>(null);
  const imageInputRef = useRef<HTMLInputElement | null>(null);
  const cameraInputRef = useRef<HTMLInputElement | null>(null);

  // Speech Recognition State
  const [isRecording, setIsRecording] = useState(false);
  const recognitionRef = useRef<any>(null);

  const abortControllerRef = useRef<AbortController | null>(null);
  const endRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, loading]);

  useEffect(() => {
    void refreshConversations();
  }, []);

  useEffect(() => {
    const timeout = window.setTimeout(() => void refreshConversations(conversationSearch), 300);
    return () => window.clearTimeout(timeout);
  }, [conversationSearch]);

  useEffect(() => {
    const handlePaste = (event: ClipboardEvent) => {
      const files = Array.from(event.clipboardData?.files ?? []);
      if (!files.length) {
        return;
      }

      void addFiles(files, 'Clipboard screenshot');
    };

    window.addEventListener('paste', handlePaste);
    return () => window.removeEventListener('paste', handlePaste);
  }, []);

  // Speech Recognition Setup
  useEffect(() => {
    const SpeechObj = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (SpeechObj) {
      const rec = new SpeechObj();
      rec.continuous = false;
      rec.interimResults = false;
      rec.lang = 'en-US';

      rec.onresult = (event: any) => {
        const text = event.results[0][0].transcript;
        setInput(prev => prev + ' ' + text);
        setIsRecording(false);
        toast.success('Speech transcribed successfully');
      };

      rec.onerror = () => {
        setIsRecording(false);
        toast.error('Voice transcription error');
      };

      rec.onend = () => {
        setIsRecording(false);
      };

      recognitionRef.current = rec;
    }
  }, []);

  const toggleRecording = () => {
    if (!recognitionRef.current) {
      toast.error('Speech recognition not supported in this browser.');
      return;
    }

    if (isRecording) {
      recognitionRef.current.stop();
    } else {
      setIsRecording(true);
      recognitionRef.current.start();
      toast.info('Listening... Speak now.');
    }
  };

  async function addFiles(files: File[], source = 'Attachment') {
    const acceptedFiles = files.filter((file) => {
      if (file.size > 20 * 1024 * 1024) {
        toast.error(`${file.name} exceeds the 20 MB limit`);
        return false;
      }

      return true;
    });

    if (!acceptedFiles.length) {
      return;
    }

    const prepared = await Promise.all(acceptedFiles.map(fileToAttachment));
    setAttachments((current) => [...current, ...prepared]);
    setPreviewOpen(prepared.some((item) => item.kind === 'image'));
    setAiMode(inferModeFromAttachments(prepared));
    toast.success(`${source} added`);
  }

  async function fileToAttachment(file: File): Promise<PendingAttachment> {
    const base64Content = await readFileAsDataUrl(file);
    const kind = detectAttachmentKind(file);
    const metadata = [`${formatFileSize(file.size)}`, statusLabel(kind)];
    const previewUrl = kind === 'image' ? base64Content : undefined;

    if (kind === 'image') {
      const dimensions = await readImageDimensions(base64Content).catch(() => null);
      if (dimensions) {
        metadata.unshift(`${dimensions.width} x ${dimensions.height}`);
      }
    }

    return {
      id: crypto.randomUUID(),
      fileName: file.name,
      contentType: file.type || guessContentType(file.name),
      base64Content,
      size: file.size,
      previewUrl,
      kind,
      progress: 100,
      status: 'ready',
      metadata
    };
  }

  function removeAttachment(id: string) {
    setAttachments((current) => current.filter((attachment) => attachment.id !== id));
    toast.info('Attachment removed');
  }

  function retryAttachment(id: string) {
    setAttachments((current) => current.map((attachment) => (
      attachment.id === id ? { ...attachment, status: 'ready', progress: 100 } : attachment
    )));
    toast.success('Attachment ready');
  }

  function handleInputFiles(event: React.ChangeEvent<HTMLInputElement>, source: string) {
    const files = Array.from(event.target.files ?? []);
    event.target.value = '';
    void addFiles(files, source);
  }

  function handleDrop(event: React.DragEvent) {
    event.preventDefault();
    setIsDraggingFiles(false);
    void addFiles(Array.from(event.dataTransfer.files), 'Dropped files');
  }

  function hasReadyAttachment() {
    return attachments.some((attachment) => attachment.status === 'ready');
  }

  async function ask(question: string, replaceMessageId?: string) {
    if ((!question && !hasReadyAttachment()) || loading) {
      return;
    }

    const readyAttachments = attachments.filter((attachment) => attachment.status === 'ready');
    const firstAttachment = readyAttachments[0];
    const prompt = question || 'Analyze the attached file.';

    const userMessage: ChatMessage = {
      id: replaceMessageId ?? crypto.randomUUID(),
      role: 'user',
      content: prompt,
      attachmentName: firstAttachment?.fileName,
      attachmentBase64: firstAttachment?.base64Content,
      attachments: readyAttachments.map((attachment) => ({
        fileName: attachment.fileName,
        contentType: attachment.contentType,
        base64Content: attachment.base64Content,
        size: attachment.size
      })),
      createdAt: new Date().toISOString()
    };

    setMessages((current) => {
      if (!replaceMessageId) {
        return [...current, userMessage];
      }

      const messageIndex = current.findIndex((message) => message.id === replaceMessageId);
      if (messageIndex < 0) {
        return [...current, userMessage];
      }

      return [...current.slice(0, messageIndex), userMessage];
    });

    setInput('');
    setEditingMessageId(null);
    setError('');
    setLoading(true);

    const currentAttachments = readyAttachments;
    setAttachments([]);
    setPreviewOpen(false);

    abortControllerRef.current = new AbortController();

    try {
      const startedAt = performance.now();
      
      const response = await chatApi.send({
        question: prompt,
        mode: aiMode,
        sessionGuid,
        attachmentBase64: currentAttachments[0]?.base64Content,
        attachmentName: currentAttachments[0]?.fileName,
        attachments: currentAttachments.map((attachment) => ({
          fileName: attachment.fileName,
          contentType: attachment.contentType,
          base64Content: attachment.base64Content,
          size: attachment.size
        })),
        targetLanguage: targetLang
      });

      if (response.sessionGuid && response.sessionGuid !== sessionGuid) {
        setSessionGuid(response.sessionGuid);
      }

      const elapsedMs = performance.now() - startedAt;
      const tempId = crypto.randomUUID();

      const assistantMessage: ChatMessage = {
        id: tempId,
        role: 'assistant',
        content: '',
        sources: response.sources,
        toolUsed: response.toolUsed,
        elapsedMs,
        confidenceScore: response.confidenceScore,
        promptTokens: response.promptTokens,
        completionTokens: response.completionTokens,
        totalTokens: response.totalTokens,
        detectedLanguage: response.detectedLanguage,
        translatedAnswer: response.translatedAnswer,
        createdAt: new Date().toISOString()
      };

      setMessages((current) => [...current, assistantMessage]);

      const fullAnswer = response.answer;
      let currentLength = 0;
      
      const interval = setInterval(() => {
        currentLength += Math.min(3, fullAnswer.length - currentLength);
        const currentChunk = fullAnswer.slice(0, currentLength);
        
        setMessages((current) => {
          return current.map(m => m.id === tempId ? { ...m, content: currentChunk } : m);
        });

        if (currentLength >= fullAnswer.length) {
          clearInterval(interval);
          setLoading(false);
        }
      }, 20);

      streamingIntervalRef.current = interval;

      await historyApi.add({
        question: prompt,
        answer: response.answer,
        toolUsed: response.toolUsed,
        createdAt: new Date().toISOString()
      });
      void refreshConversations();
    } catch (err) {
      if ((err as any).name === 'AbortError') {
        toast.info('Generation stopped');
      } else {
        const message = err instanceof Error ? err.message : 'Chat request failed.';
        setError(message);
        setMessages((current) => [
          ...current,
          {
            id: crypto.randomUUID(),
            role: 'assistant',
            content: message,
            isError: true,
            toolUsed: 'Error',
            createdAt: new Date().toISOString()
          }
        ]);
      }
    } finally {
      if (!streamingIntervalRef.current) {
        setLoading(false);
      }
    }
  }

  useEffect(() => {
    return () => {
      if (streamingIntervalRef.current) {
        clearInterval(streamingIntervalRef.current);
      }
    };
  }, []);

  const stopGeneration = () => {
    if (streamingIntervalRef.current) {
      clearInterval(streamingIntervalRef.current);
    }
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    setLoading(false);
  };

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const question = input.trim();

    if (!question && !hasReadyAttachment()) {
      return;
    }

    void ask(question, editingMessageId ?? undefined);
  }

  function editQuestion(message: ChatMessage) {
    setInput(message.content);
    setEditingMessageId(message.id);
  }

  function regenerateAnswer(message: ChatMessage) {
    const index = messages.findIndex((item) => item.id === message.id);
    const previousQuestion = [...messages.slice(0, index)].reverse().find((item) => item.role === 'user');
    if (!previousQuestion) {
      toast.info('No question found to regenerate.');
      return;
    }

    void ask(previousQuestion.content, previousQuestion.id);
  }

  function downloadChat() {
    const transcript = messages
      .filter((message) => message.id !== 'welcome')
      .map((message) => `## ${message.role === 'user' ? 'Question' : 'Answer'}\n\n${message.content}`)
      .join('\n\n---\n\n');

    if (!transcript) {
      toast.info('No chat to download yet.');
      return;
    }

    const blob = new Blob([transcript], { type: 'text/markdown;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `copilot-workspace-chat-${Date.now()}.md`;
    anchor.click();
    URL.revokeObjectURL(url);
    toast.success('Chat transcript exported');
  }

  function clearChat() {
    setMessages([welcomeMessage]);
    setEditingMessageId(null);
    setSessionGuid(crypto.randomUUID());
    setInput('');
    setAttachments([]);
    setPreviewOpen(false);
    toast.info('Chat session cleared');
  }

  async function startNewConversation() {
    try {
      const session = await chatApi.createSession('New Chat');
      setSessionGuid(session.sessionGuid);
      setMessages([welcomeMessage]);
      setInput('');
      setAttachments([]);
      setEditingMessageId(null);
      await refreshConversations();
      toast.success('New conversation started');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to create conversation');
    }
  }

  async function refreshConversations(search = conversationSearch) {
    try {
      const sessions = await chatApi.listSessions({ search: search || undefined, pageSize: 50 });
      setConversations(sessions);
    } catch {
      setConversations([]);
    }
  }

  async function openConversation(conversation: ConversationSession) {
    try {
      const rows = await chatApi.loadMessages(conversation.sessionGuid);
      const loadedMessages = rows
        .slice()
        .reverse()
        .map<ChatMessage>((row) => {
          const dto = row as any;
          const role = dto.Role ?? dto.role;
          const message = dto.Message ?? dto.message ?? '';

          return {
            id: String(dto.MessageId ?? dto.messageId ?? crypto.randomUUID()),
            role: role === 'Assistant' ? 'assistant' : 'user',
            content: role === 'Assistant'
              ? normalizeLegacyAssistantText(message) ?? message
              : message,
            createdAt: dto.CreatedDate ?? dto.createdDate ?? new Date().toISOString()
          };
        });

      setSessionGuid(conversation.sessionGuid);
      setMessages(loadedMessages.length ? loadedMessages : [welcomeMessage]);
      setInput('');
      setEditingMessageId(null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to load conversation');
    }
  }

  async function renameConversation(conversation: ConversationSession) {
    const title = window.prompt('Conversation title', conversation.title)?.trim();
    if (!title || title === conversation.title) return;
    await chatApi.updateSession(conversation.sessionGuid, { title });
    await refreshConversations();
  }

  async function togglePin(conversation: ConversationSession) {
    await chatApi.updateSession(conversation.sessionGuid, { isPinned: !conversation.isPinned });
    await refreshConversations();
  }

  async function toggleFavorite(conversation: ConversationSession) {
    await chatApi.updateSession(conversation.sessionGuid, { isFavorite: !conversation.isFavorite });
    await refreshConversations();
  }

  async function deleteConversation(conversation: ConversationSession) {
    if (!window.confirm(`Delete "${conversation.title}"?`)) return;
    await chatApi.deleteSession(conversation.sessionGuid);
    if (conversation.sessionGuid === sessionGuid) {
      clearChat();
    }
    await refreshConversations();
  }

  const previewAttachment = attachments.find((attachment) => attachment.kind === 'image' && attachment.previewUrl);

  return (
    <motion.div
      initial={{ opacity: 0, y: 15 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -15 }}
      transition={{ duration: 0.35, ease: 'easeOut' }}
    >
      <Box>
      <PageHeader
        actions={
          <Stack direction="row" spacing={1}>
            <Button onClick={downloadChat} startIcon={<DownloadOutlinedIcon />} variant="outlined" sx={{ borderRadius: 3 }}>
              Export
            </Button>
            <Button color="error" onClick={clearChat} startIcon={<DeleteOutlineOutlinedIcon />} variant="outlined" sx={{ borderRadius: 3 }}>
              Clear
            </Button>
          </Stack>
        }
        eyebrow="AI Assistant"
        title="Copilot Workspace"
        description="Interact with the knowledge engine, scan multimodal attachments, review structured DB procedures, and audit metrics."
      />

      {error && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
          {error}
        </Alert>
      )}

      {/* Main Workspace Panels layout */}
      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 3 }}>
          <Card sx={{ height: 'calc(100vh - 220px)', minHeight: 580, borderRadius: 2, display: 'flex', flexDirection: 'column' }}>
            <Box sx={{ p: 2, borderBottom: '1px solid', borderColor: 'divider' }}>
              <Button fullWidth variant="contained" startIcon={<AddCommentOutlinedIcon />} onClick={startNewConversation} sx={{ borderRadius: 2, mb: 1.5 }}>
                New Chat
              </Button>
              <Paper variant="outlined" sx={{ px: 1.25, py: 0.5, display: 'flex', alignItems: 'center', gap: 1, borderRadius: 2 }}>
                <SearchOutlinedIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                <InputBase fullWidth value={conversationSearch} onChange={(event) => setConversationSearch(event.target.value)} placeholder="Search conversations" sx={{ fontSize: '0.875rem' }} />
              </Paper>
            </Box>
            <Stack spacing={0.75} sx={{ p: 1, overflowY: 'auto', flex: 1 }}>
              {conversations.map((conversation) => (
                <Paper
                  key={conversation.sessionGuid}
                  variant="outlined"
                  onClick={() => void openConversation(conversation)}
                  sx={{
                    p: 1.25,
                    borderRadius: 2,
                    cursor: 'pointer',
                    borderColor: conversation.sessionGuid === sessionGuid ? 'primary.main' : 'divider',
                    bgcolor: conversation.sessionGuid === sessionGuid ? alpha(theme.palette.primary.main, 0.06) : 'background.paper'
                  }}
                >
                  <Stack direction="row" alignItems="center" spacing={0.5}>
                    <Typography noWrap fontWeight={700} variant="body2" sx={{ flex: 1 }}>{conversation.title}</Typography>
                    <Tooltip title={conversation.isPinned ? 'Unpin' : 'Pin'}>
                      <IconButton size="small" onClick={(event) => { event.stopPropagation(); void togglePin(conversation); }}>
                        <PushPinOutlinedIcon sx={{ fontSize: 16, color: conversation.isPinned ? 'primary.main' : 'text.secondary' }} />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title={conversation.isFavorite ? 'Remove favorite' : 'Favorite'}>
                      <IconButton size="small" onClick={(event) => { event.stopPropagation(); void toggleFavorite(conversation); }}>
                        {conversation.isFavorite ? <StarOutlinedIcon sx={{ fontSize: 16, color: 'warning.main' }} /> : <StarBorderOutlinedIcon sx={{ fontSize: 16 }} />}
                      </IconButton>
                    </Tooltip>
                  </Stack>
                  <Typography color="text.secondary" noWrap variant="caption" sx={{ display: 'block', mt: 0.25 }}>
                    {conversation.lastMessagePreview || new Date(conversation.updatedDate).toLocaleString()}
                  </Typography>
                  <Stack direction="row" spacing={0.5} sx={{ mt: 0.75 }}>
                    <Button size="small" onClick={(event) => { event.stopPropagation(); void renameConversation(conversation); }}>Rename</Button>
                    <Button color="error" size="small" onClick={(event) => { event.stopPropagation(); void deleteConversation(conversation); }}>Delete</Button>
                  </Stack>
                </Paper>
              ))}
            </Stack>
          </Card>
        </Grid>

        {/* Left/Middle Chat Workspace */}
        <Grid size={{ xs: 12, lg: previewOpen ? 6 : 9 }}>
          <Card 
            sx={{ 
              height: 'calc(100vh - 220px)', 
              minHeight: 580, 
              display: 'flex', 
              flexDirection: 'column', 
              borderRadius: 4,
              overflow: 'hidden'
            }}
          >
            {/* Header / Mode Controls */}
            <Box 
              sx={{ 
                borderBottom: '1px solid', 
                borderColor: 'divider', 
                px: 3, 
                py: 2,
                bgcolor: theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.01)' : 'rgba(0, 0, 0, 0.01)'
              }}
            >
              <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems="center" spacing={2}>
                <Stack direction="row" alignItems="center" spacing={1.5}>
                  <PsychologyOutlinedIcon color="primary" sx={{ fontSize: 26 }} />
                  <Box>
                    <Typography fontWeight={800} variant="body1">Cognitive Engine Hub</Typography>
                    <Typography color="text.secondary" variant="caption" sx={{ fontWeight: 500 }}>
                      Automatic language detection, vector matching, and layout OCR
                    </Typography>
                  </Box>
                </Stack>

                {/* Switchers */}
                <Stack direction="row" alignItems="center" spacing={1.5}>
                  <FormControl size="small" sx={{ minWidth: 160 }}>
                    <Select
                      value={aiMode}
                      onChange={(e) => setAiMode(e.target.value)}
                      sx={{ borderRadius: 3, fontSize: '0.85rem', fontWeight: 600 }}
                    >
                      {aiModes.map(mode => (
                        <MenuItem key={mode.value} value={mode.value} sx={{ fontSize: '0.85rem' }}>
                          {mode.label}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>

                  <FormControl size="small" sx={{ minWidth: 120 }}>
                    <Select
                      value={targetLang}
                      onChange={(e) => setTargetLang(e.target.value)}
                      startAdornment={<TranslateIcon sx={{ fontSize: 16, mr: 1, color: 'text.secondary' }} />}
                      sx={{ borderRadius: 3, fontSize: '0.85rem', fontWeight: 600 }}
                    >
                      {languages.map(lang => (
                        <MenuItem key={lang.code} value={lang.code} sx={{ fontSize: '0.85rem' }}>
                          {lang.name}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Stack>
              </Stack>
            </Box>

            {/* Quick Suggestions & Prompt Templates Library */}
            {messages.length === 1 && (
              <Box sx={{ px: 3, pt: 2 }}>
                <Stack direction="row" gap={1.25} flexWrap="wrap" sx={{ mb: 2.5 }}>
                  {quickPrompts.map((prompt) => (
                    <Chip
                      clickable
                      disabled={loading}
                      key={prompt}
                      label={prompt}
                      onClick={() => void ask(prompt)}
                      sx={{ borderRadius: 3, py: 1.5, fontWeight: 500, fontSize: '0.8rem' }}
                    />
                  ))}
                </Stack>

                <Typography variant="caption" fontWeight={700} sx={{ display: 'block', mb: 1.5, textTransform: 'uppercase', color: 'text.secondary', letterSpacing: '0.05em' }}>
                  💡 Enterprise Prompt Templates Library
                </Typography>
                <Grid container spacing={2}>
                  {promptTemplates.map((tmpl) => (
                    <Grid size={{ xs: 12, sm: 6 }} key={tmpl.label}>
                      <motion.div
                        whileHover={{ y: -4, scale: 1.015 }}
                        whileTap={{ scale: 0.995 }}
                        transition={{ duration: 0.2 }}
                      >
                        <Paper 
                          variant="outlined" 
                          onClick={() => { setInput(tmpl.text); setAiMode(tmpl.label.includes('💻') ? 'Developer' : tmpl.label.includes('📑') ? 'Ocr' : 'SmartAuto'); }}
                          sx={{ 
                            p: 2, 
                            borderRadius: 3, 
                            cursor: 'pointer', 
                            border: '1px solid',
                            borderColor: 'divider',
                            backgroundColor: theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.01)' : 'rgba(0,0,0,0.01)',
                            '&:hover': { 
                              borderColor: 'primary.main', 
                              backgroundColor: theme.palette.mode === 'dark' ? 'rgba(99,102,241,0.05)' : 'rgba(79,70,229,0.02)' 
                            },
                            transition: 'all 0.2s ease-in-out'
                          }}
                        >
                          <Typography variant="body2" fontWeight={700} color="primary.main">{tmpl.label}</Typography>
                          <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block', mt: 0.5 }}>{tmpl.text}</Typography>
                        </Paper>
                      </motion.div>
                    </Grid>
                  ))}
                </Grid>
              </Box>
            )}

            {/* Conversations Feed */}
            <Stack spacing={3} sx={{ flex: 1, overflowY: 'auto', p: 3 }}>
              {messages.map((message) => (
                <ChatMessageItem
                  key={message.id}
                  message={message}
                  onEdit={message.role === 'user' ? editQuestion : undefined}
                  onRegenerate={message.role === 'assistant' && message.id !== 'welcome' ? regenerateAnswer : undefined}
                />
              ))}
              {loading && (
                <Stack direction="row" justifyContent="flex-start">
                  <Paper elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 3, p: 2.5, bgcolor: 'background.paper' }}>
                    <TypingIndicator />
                  </Paper>
                </Stack>
              )}
              <div ref={endRef} />
            </Stack>

            {/* Prompts Input area */}
            <Box component="form" onSubmit={handleSubmit} sx={{ borderTop: '1px solid', borderColor: 'divider', p: 3 }}>
              {/* Editing Banner */}
              {editingMessageId && (
                <Stack alignItems="center" direction="row" spacing={1} sx={{ mb: 1.5 }}>
                  <EditOutlinedIcon color="primary" fontSize="small" />
                  <Typography color="text.secondary" variant="caption" sx={{ fontWeight: 600 }}>
                    Editing a previous question. Submitting will update the conversation sequence.
                  </Typography>
                  <Button
                    onClick={() => {
                      setEditingMessageId(null);
                      setInput('');
                    }}
                    size="small"
                  >
                    Cancel
                  </Button>
                </Stack>
              )}

              <Paper
                onDragEnter={(event) => {
                  event.preventDefault();
                  setIsDraggingFiles(true);
                }}
                onDragOver={(event) => event.preventDefault()}
                onDragLeave={() => setIsDraggingFiles(false)}
                onDrop={handleDrop}
                sx={{
                  p: 1.5,
                  borderRadius: 4,
                  border: '1px solid',
                  borderColor: isDraggingFiles ? 'primary.main' : 'divider',
                  bgcolor: isDraggingFiles
                    ? 'rgba(99, 102, 241, 0.08)'
                    : theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.02)' : 'rgba(0, 0, 0, 0.01)',
                  boxShadow: 'none',
                  transition: 'border-color 0.25s, box-shadow 0.25s',
                  '&:focus-within': {
                    borderColor: 'primary.main',
                    boxShadow: theme.palette.mode === 'dark' 
                      ? '0 0 16px rgba(99, 102, 241, 0.2)' 
                      : '0 0 16px rgba(79, 70, 229, 0.1)'
                  }
                }}
              >
                <input
                  accept={documentAccept}
                  style={{ display: 'none' }}
                  ref={documentInputRef}
                  type="file"
                  multiple
                  onChange={(event) => handleInputFiles(event, 'Files')}
                />
                <input
                  accept={imageAccept}
                  style={{ display: 'none' }}
                  ref={imageInputRef}
                  type="file"
                  multiple
                  onChange={(event) => handleInputFiles(event, 'Images')}
                />
                <input
                  accept={imageAccept}
                  capture="environment"
                  style={{ display: 'none' }}
                  ref={cameraInputRef}
                  type="file"
                  onChange={(event) => handleInputFiles(event, 'Camera image')}
                />

                {attachments.length > 0 && (
                  <Stack direction="row" flexWrap="wrap" gap={1.25} sx={{ mb: 1.5 }}>
                    {attachments.map((attachment) => (
                      <Paper
                        key={attachment.id}
                        variant="outlined"
                        sx={{
                          width: { xs: '100%', sm: 260 },
                          p: 1,
                          borderRadius: 2,
                          display: 'flex',
                          gap: 1,
                          alignItems: 'center',
                          bgcolor: 'background.paper'
                        }}
                      >
                        <Box sx={{ width: 44, height: 44, borderRadius: 1.5, bgcolor: 'action.hover', display: 'grid', placeItems: 'center', overflow: 'hidden', color: 'primary.main' }}>
                          {attachment.previewUrl ? (
                            <Box component="img" src={attachment.previewUrl} alt={attachment.fileName} sx={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                          ) : (
                            <AttachmentKindIcon kind={attachment.kind} />
                          )}
                        </Box>
                        <Box sx={{ minWidth: 0, flex: 1 }}>
                          <Typography variant="body2" noWrap fontWeight={700}>{attachment.fileName}</Typography>
                          <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
                            {attachment.metadata.join(' · ')}
                          </Typography>
                          {attachment.progress < 100 && (
                            <Box sx={{ height: 4, mt: 0.75, borderRadius: 999, bgcolor: 'divider', overflow: 'hidden' }}>
                              <Box sx={{ height: '100%', width: `${attachment.progress}%`, bgcolor: 'primary.main' }} />
                            </Box>
                          )}
                        </Box>
                        {attachment.status === 'failed' && (
                          <Button size="small" onClick={() => retryAttachment(attachment.id)}>Retry</Button>
                        )}
                        <IconButton size="small" aria-label={`Remove ${attachment.fileName}`} onClick={() => removeAttachment(attachment.id)}>
                          <CloseIcon fontSize="small" />
                        </IconButton>
                      </Paper>
                    ))}
                  </Stack>
                )}

                <InputBase
                  disabled={loading}
                  fullWidth
                  multiline
                  maxRows={4}
                  onChange={(event) => setInput(event.target.value)}
                  placeholder={
                    aiMode === 'Database' ? "Ask about tables, indexes, schemas or write a query..." :
                    aiMode === 'Ocr' ? "Attach a medical file, receipt, or invoice for OCR analysis..." :
                    "Ask anything..."
                  }
                  value={input}
                  sx={{ fontSize: '0.975rem', px: 1, py: 0.75 }}
                />

                <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1} sx={{ pt: 1, borderTop: '1px solid', borderColor: 'divider' }}>
                  <Stack direction="row" spacing={0.75}>
                    <Tooltip title="Attach file">
                      <span>
                        <IconButton disabled={loading} onClick={() => documentInputRef.current?.click()} size="small" sx={{ bgcolor: 'divider', borderRadius: 3 }}>
                          <AttachFileIcon sx={{ fontSize: 20 }} />
                        </IconButton>
                      </span>
                    </Tooltip>
                    <Tooltip title="Upload image">
                      <span>
                        <IconButton disabled={loading} onClick={() => imageInputRef.current?.click()} size="small" sx={{ bgcolor: 'divider', borderRadius: 3 }}>
                          <ImageOutlinedIcon sx={{ fontSize: 20 }} />
                        </IconButton>
                      </span>
                    </Tooltip>
                    <Tooltip title="Camera">
                      <span>
                        <IconButton disabled={loading} onClick={() => cameraInputRef.current?.click()} size="small" sx={{ bgcolor: 'divider', borderRadius: 3 }}>
                          <PhotoCameraOutlinedIcon sx={{ fontSize: 20 }} />
                        </IconButton>
                      </span>
                    </Tooltip>
                    <Tooltip title={isRecording ? "Stop voice dictation" : "Dictate using voice"}>
                      <IconButton 
                        onClick={toggleRecording} 
                        disabled={loading} 
                        size="small" 
                        sx={{ 
                          bgcolor: isRecording ? 'error.main' : 'divider', 
                          borderRadius: 3, 
                          color: isRecording ? '#ffffff' : 'text.primary' 
                        }}
                      >
                        {isRecording ? <MicOffIcon sx={{ fontSize: 20 }} /> : <MicIcon sx={{ fontSize: 20 }} />}
                      </IconButton>
                    </Tooltip>
                  </Stack>

                  <Stack direction="row" spacing={0.75}>
                    {loading && (
                      <Tooltip title="Stop generation">
                        <IconButton color="error" onClick={stopGeneration} size="small">
                          <StopCircleOutlinedIcon sx={{ fontSize: 24 }} />
                        </IconButton>
                      </Tooltip>
                    )}
                    <Tooltip title="Submit prompt">
                      <span>
                        <IconButton color="primary" disabled={loading || (!input.trim() && !hasReadyAttachment())} type="submit" size="small" sx={{ bgcolor: 'primary.main', color: '#ffffff', borderRadius: 3, p: 1, '&:hover': { bgcolor: 'primary.dark' } }}>
                          <SendOutlinedIcon sx={{ fontSize: 20 }} />
                        </IconButton>
                      </span>
                    </Tooltip>
                  </Stack>
                </Stack>
              </Paper>
            </Box>
          </Card>
        </Grid>

        {/* Right Side Attachment Preview Panel */}
        {previewOpen && previewAttachment && (
          <Grid size={{ xs: 12, lg: 4 }}>
            <Card sx={{ height: 'calc(100vh - 220px)', minHeight: 580, borderRadius: 4, display: 'flex', flexDirection: 'column' }}>
              <Box sx={{ borderBottom: '1px solid', borderColor: 'divider', px: 3, py: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center', bgcolor: 'action.hover' }}>
                <Stack direction="row" alignItems="center" spacing={1}>
                  <PreviewIcon color="primary" sx={{ fontSize: 20 }} />
                  <Typography fontWeight={800} variant="body2">Resource Viewer</Typography>
                </Stack>
                <IconButton size="small" onClick={() => setPreviewOpen(false)}>
                  ×
                </IconButton>
              </Box>
              <Box sx={{ flexGrow: 1, p: 3, display: 'flex', justifyContent: 'center', alignItems: 'center', bgcolor: '#0b0f19', overflow: 'hidden' }}>
                {previewAttachment.previewUrl ? (
                  <Box 
                    component="img" 
                    src={previewAttachment.previewUrl} 
                    alt="Multi-modal upload attachment" 
                    sx={{ 
                      maxWidth: '100%', 
                      maxHeight: '100%', 
                      objectFit: 'contain', 
                      borderRadius: 2, 
                      boxShadow: '0 8px 30px rgba(0,0,0,0.5)' 
                    }} 
                  />
                ) : (
                  <Box sx={{ width: '100%', height: '100%', p: 2, borderRadius: 2, bgcolor: '#121824', color: 'grey.300', overflowY: 'auto', fontFamily: 'monospace', fontSize: '0.825rem' }}>
                    <Typography variant="body2" sx={{ fontWeight: 700, color: 'primary.main', mb: 1 }}>
                      [Attached Scanned File]
                    </Typography>
                    {previewAttachment.fileName}
                  </Box>
                )}
              </Box>
            </Card>
          </Grid>
        )}
      </Grid>
      </Box>
    </motion.div>
  );
}
