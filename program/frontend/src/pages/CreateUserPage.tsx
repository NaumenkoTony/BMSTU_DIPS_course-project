import { useState } from "react";
import { Container, TextInput, PasswordInput, Button, Select, Alert, LoadingOverlay } from "@mantine/core";
import { useForm } from "@mantine/form";
import { IconCheck, IconX } from "@tabler/icons-react";
import { createUser, type CreateUserRequest } from "../api/AdminClient";
import "./CreateUserPage.css";

interface CreateUserForm extends CreateUserRequest {}

export default function CreateUserPage() {
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

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
      password: (value) => value.length < 6 ? 'Пароль должен быть не менее 6 символов, одной заглавной буквы и спецсимвола' : null,
    },
  });

  const handleSubmit = async (values: CreateUserForm) => {
    setLoading(true);
    setMessage(null);

    try {
      console.log(values)
      const result = await createUser(values);
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
    <Container className="create-user-container">
      <LoadingOverlay visible={loading} />
      
      <h1 className="create-user-title">Создание пользователя</h1>
      
      {message && (
        <Alert 
          color={message.type === 'success' ? 'green' : 'red'} 
          icon={message.type === 'success' ? <IconCheck /> : <IconX />}
          className="create-user-alert"
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
            className="create-user-input"
            {...form.getInputProps('username')}
          />

          <TextInput
            label="Email"
            placeholder="user@example.com"
            type="email"
            required
            className="create-user-input"
            {...form.getInputProps('email')}
          />

          <TextInput
            label="Имя"
            placeholder="Иван"
            required
            className="create-user-input"
            {...form.getInputProps('firstName')}
          />

          <TextInput
            label="Фамилия"
            placeholder="Иванов"
            required
            className="create-user-input"
            {...form.getInputProps('lastName')}
          />

          <PasswordInput
            label="Пароль"
            placeholder="Не менее 6 символов, одной заглавной буквы и спецсимвола"
            required
            className="create-user-input"
            {...form.getInputProps('password')}
          />

          <Select
            label="Роли"
            placeholder="Выберите роли"
            data={['User', 'Admin']}
            defaultValue={['User']}
            multiple
            className="create-user-input"
            {...form.getInputProps('roles')}
          />

          <Button 
            type="submit" 
            loading={loading} 
            fullWidth 
            className="create-user-button"
          >
            Создать пользователя
          </Button>
        </form>
      </div>
    </Container>
  );
}