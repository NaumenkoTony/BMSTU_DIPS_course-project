import { useState, useEffect } from "react";
import { Container, TextInput, PasswordInput, Button, Select, Alert, LoadingOverlay, Paper, Loader } from "@mantine/core";
import { useForm } from "@mantine/form";
import { IconCheck, IconX } from "@tabler/icons-react";
import { createUser, type CreateUserRequest } from "../api/AdminClient";
import { getAvailableRoles } from "../api/AdminClient";
import "./CreateUserPage.css";

interface CreateUserForm extends CreateUserRequest { }

export default function CreateUserPage() {
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [availableRoles, setAvailableRoles] = useState<string[]>([]);
  const [rolesLoading, setRolesLoading] = useState(true);
  const [rolesError, setRolesError] = useState<string | null>(null);

  useEffect(() => {
    const loadRoles = async () => {
      try {
        setRolesLoading(true);
        const roles = await getAvailableRoles();
        setAvailableRoles(roles);
      } catch (error) {
        setRolesError(error instanceof Error ? error.message : 'Ошибка загрузки ролей');
        console.error('Failed to load roles:', error);
      } finally {
        setRolesLoading(false);
      }
    };

    loadRoles();
  }, []);

  const form = useForm<CreateUserForm>({
    initialValues: {
      username: '',
      email: '',
      firstName: '',
      lastName: '',
      password: '',
      roles: ['User']
    },
    validate: {
      username: (value) => value.length < 3 ? 'Имя пользователя слишком короткое' : null,
      email: (value) => !/^\S+@\S+$/.test(value) ? 'Некорректный email' : null,
      firstName: (value) => value.length < 2 ? 'Имя слишком короткое' : null,
      lastName: (value) => value.length < 2 ? 'Фамилия слишком короткая' : null,
      password: (value) => value.length < 6 ? 'Пароль должен быть не менее 6 символов, заглавной буквы и спецсимвола' : null,
    },
  });

  const handleSubmit = async (values: CreateUserForm) => {
    setLoading(true);
    setMessage(null);

    try {
      const payload: CreateUserForm = {
        ...values,
        roles: Array.isArray(values.roles) ? values.roles : [values.roles],
      };

      console.log('Submitting user:', payload);
      const result = await createUser(payload);
      setMessage({ type: 'success', text: result.message });
      form.reset();
    } catch (error) {
      setMessage({
        type: 'error',
        text: error instanceof Error ? error.message : 'Неизвестная ошибка'
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="create-user-page">
      <Container size="sm">
        <Paper shadow="lg" p="xl" radius="lg" className="create-user-card">
          <div className="create-user-header">
            <div className="create-user-title">
              Создание пользователя
            </div>
          </div>

          <LoadingOverlay visible={loading} />

          {message && (
            <Alert
              color={message.type === 'success' ? 'green' : 'red'}
              icon={message.type === 'success' ? <IconCheck /> : <IconX />}
              className="create-user-alert"
              radius="md"
            >
              {message.text}
            </Alert>
          )}

          <div className="create-user-form">
            <form onSubmit={form.onSubmit(handleSubmit)}>
              <TextInput
                label="Имя пользователя"
                placeholder="username"
                required
                size="md"
                className="create-user-input"
                {...form.getInputProps('username')}
              />

              <TextInput
                label="Email"
                placeholder="user@example.com"
                type="email"
                required
                size="md"
                className="create-user-input"
                {...form.getInputProps('email')}
              />

              <TextInput
                label="Имя"
                placeholder="Иван"
                required
                size="md"
                className="create-user-input"
                {...form.getInputProps('firstName')}
              />

              <TextInput
                label="Фамилия"
                placeholder="Иванов"
                required
                size="md"
                className="create-user-input"
                {...form.getInputProps('lastName')}
              />

              <PasswordInput
                label="Пароль"
                placeholder="Не менее 6 символов, заглавной буквы и спецсимвола"
                required
                size="md"
                className="create-user-input"
                {...form.getInputProps('password')}
              />

              {rolesLoading ? (
                <div className="roles-loading">
                  <Loader size="sm" />
                  <span>Загрузка ролей...</span>
                </div>
              ) : rolesError ? (
                <Alert color="yellow" className="roles-error">
                  Не удалось загрузить роли: {rolesError}
                </Alert>
              ) : (
                <Select
                  label="Роли"
                  placeholder="Выберите роли"
                  data={availableRoles} // Используем загруженные роли
                  defaultValue={['User']}
                  multiple
                  className="create-user-input"
                  {...form.getInputProps('roles')}
                />
              )}

              <Button
                type="submit"
                loading={loading}
                fullWidth
                size="lg"
                className="create-user-button"
                styles={{
                  root: {
                    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                    border: 'none',
                    borderRadius: '8px',
                    fontWeight: '600',
                    height: '48px'
                  }
                }}
              >
                Создать пользователя
              </Button>
            </form>
          </div>
        </Paper>
      </Container>
    </div>
  );
}