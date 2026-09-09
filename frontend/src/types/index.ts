export interface SocialLinks {
  websiteUrl: string | null;
  twitterUrl: string | null;
  facebookUrl: string | null;
  githubUrl: string | null;
  linkedinUrl: string | null;
  youtubeUrl: string | null;
  tiktokUrl: string | null;
}

export interface PublicProfile {
  id: string;
  username: string;
  displayName: string;
  bio: string | null;
  aboutMe: string | null;
  profileImageUrl: string | null;
  questionCount: number;
  totalAnswers: number;
  totalVotes: number;
  socialLinks: SocialLinks;
}

export interface MeDto {
  id: string;
  username: string;
  email: string;
  displayName: string;
  bio: string | null;
  aboutMe: string | null;
  profileImageUrl: string | null;
  role: string;
  hasPassword: boolean;
  socialLinks: SocialLinks;
}

export type QuestionSort = "Votes" | "Recent";

export interface FollowUpDto {
  id: string;
  content: string;
  createdAt: string;
}

export interface ReferencedAnswerDto {
  answerId: string;
  questionId: string;
  questionContent: string;
  answerContent: string;
}

export interface AnswerDto {
  id: string;
  content: string;
  imageUrl: string | null;
  youtubeEmbedUrl: string | null;
  createdAt: string;
  referencedAnswer: ReferencedAnswerDto | null;
}

export interface QuestionDto {
  id: string;
  content: string;
  isAnonymous: boolean;
  authorUsername: string | null;
  authorDisplayName: string | null;
  voteCount: number;
  hasVoted: boolean;
  createdAt: string;
  isVisible: boolean;
  answer: AnswerDto | null;
  followUps: FollowUpDto[];
}

export interface MyAnswerSummary {
  answerId: string;
  questionId: string;
  questionContent: string;
  answerContent: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  hasMore: boolean;
}

export interface AdminQuestionDto {
  id: string;
  targetUsername: string;
  content: string;
  isAnonymous: boolean;
  isVisible: boolean;
  isDeleted: boolean;
  voteCount: number;
  createdAt: string;
}

export interface AdminUserDto {
  id: string;
  username: string;
  displayName: string;
  email: string;
  role: string;
  questionCount: number;
  createdAt: string;
  registrationIp: string | null;
}

export interface BlockedIpDto {
  id: string;
  ipAddress: string;
  reason: string | null;
  createdAt: string;
}

export interface SecurityMetadataDto {
  questionId: string;
  sourceIp: string;
  userAgent: string | null;
  authenticatedUserId: string | null;
  submittedAt: string;
}

export interface PlatformStatsDto {
  totalUsers: number;
  totalQuestions: number;
  totalAnswers: number;
  hiddenQuestions: number;
  questionsLast7Days: number;
}

export interface RegisterResultDto {
  email: string;
  message: string;
}

export interface SearchResult {
  username: string;
  displayName: string;
  profileImageUrl: string | null;
}
